﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using DevHome.Dashboard.Services;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DevHome.Dashboard.ComSafeWidgetObjects;

/// <summary>
/// Since WidgetDefinitions are OOP COM objects, we need to wrap them in a safe way to handle COM exceptions
/// that arise when the underlying OOP object vanishes. All WidgetDefinitions should be wrapped in a
/// ComSafeWidgetDefinition and calls to the WidgetDefinition should be done through the ComSafeWidgetDefinition.
/// This class will handle the COM exceptions and get a new OOP WidgetDefinition if needed.
/// All APIs on the IWidgetDefinition and IWidgetDefinition2 interfaces are reflected here.
/// </summary>
public partial class ComSafeWidgetDefinition(string widgetDefinitionId) : IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComSafeWidgetDefinition));

    public bool AllowMultiple { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string DisplayTitle { get; private set; } = string.Empty;

    public string Id { get; private set; } = widgetDefinitionId;

    public WidgetProviderDefinition ProviderDefinition { get; private set; } = null!;

    // Since the ProviderDefinition could have died, save the display name and id so we can use it in that case.
    // These can be removed if we keep a ComSafeWidgetProviderDefinition here.
    public string ProviderDefinitionDisplayName { get; private set; } = string.Empty;

    public string ProviderDefinitionId { get; private set; } = string.Empty;

    public string AdditionalInfoUri { get; private set; } = string.Empty;

    public bool IsCustomizable { get; private set; }

    public string ProgressiveWebAppHostPackageFamilyName => throw new NotImplementedException();

    public WidgetType Type { get; private set; }

    private WidgetDefinition _oopWidgetDefinition = null!;

    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    private const int RpcCallFailed = unchecked((int)0x800706BE);

    private readonly SemaphoreSlim _getDefinitionLock = new(1, 1);
    private bool _disposedValue;

    private bool _hasValidProperties;
    private const int MaxAttempts = 3;

    /// <summary>
    /// ComSafeWidgetDefinitions must be populated before use to guarantee their properties are valid.
    /// Calling methods will populate the object, but referencing properties cannot.
    /// </summary>
    /// <returns>true if the ComSafeWidgetDefinition was successfully populated, false if not.</returns>
    public async Task<bool> PopulateAsync()
    {
        await LazilyLoadOopWidgetDefinitionAsync();
        return _hasValidProperties;
    }

    /// <summary>
    /// Gets the theme resources from the widget. Tries multiple times in case of COM exceptions.
    /// </summary>
    /// <returns>The theme resources, or null in the case of failure.</returns>
    public async Task<WidgetThemeResources> GetThemeResourceAsync(WidgetTheme theme)
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                await LazilyLoadOopWidgetDefinitionAsync();
                return await Task.Run(() => _oopWidgetDefinition.GetThemeResource(theme));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                ResetOopWidgetDefinition();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception getting theme resources from widget:");
                return null!;
            }
        }

        return null!;
    }

    public async Task<WidgetCapability[]> GetWidgetCapabilitiesAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                await LazilyLoadOopWidgetDefinitionAsync();
                return await Task.Run(() => _oopWidgetDefinition.GetWidgetCapabilities());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                ResetOopWidgetDefinition();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception getting widget capabilities from widget:");
                return null!;
            }
        }

        return null!;
    }

    private void ResetOopWidgetDefinition()
    {
        _oopWidgetDefinition = null!;
        _hasValidProperties = false;
    }

    private async Task LazilyLoadOopWidgetDefinitionAsync()
    {
        var attempt = 0;
        await _getDefinitionLock.WaitAsync();
        try
        {
            while (attempt++ < 3 && (_oopWidgetDefinition == null || _hasValidProperties == false))
            {
                try
                {
                    _oopWidgetDefinition ??= await DependencyExtensions.GetRequiredService<IWidgetHostingService>().GetWidgetDefinitionAsync(Id);

                    if (!_hasValidProperties)
                    {
                        await Task.Run(() =>
                        {
                            AllowMultiple = _oopWidgetDefinition.AllowMultiple;
                            Description = _oopWidgetDefinition.Description;
                            DisplayTitle = _oopWidgetDefinition.DisplayTitle;
                            Id = _oopWidgetDefinition.Id;
                            ProviderDefinition = _oopWidgetDefinition.ProviderDefinition;
                            ProviderDefinitionDisplayName = _oopWidgetDefinition.ProviderDefinition.DisplayName;
                            ProviderDefinitionId = _oopWidgetDefinition.ProviderDefinition.Id;
                            AdditionalInfoUri = _oopWidgetDefinition.AdditionalInfoUri;
                            IsCustomizable = _oopWidgetDefinition.IsCustomizable;
                            Type = _oopWidgetDefinition.Type;
                            _hasValidProperties = true;
                        });
                    }
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to get properties of out-of-proc object");
                }
            }
        }
        finally
        {
            _getDefinitionLock.Release();
        }
    }

    /// <summary>
    /// Get a WidgetDefinition's ID from a WidgetDefinition object.
    /// </summary>
    /// <param name="widgetDefinition">WidgetDefinition</param>
    /// <returns>The WidgetDefinition's Id, or in the case of failure string.Empty</returns>
    public static async Task<string> GetIdFromUnsafeWidgetDefinitionAsync(WidgetDefinition widgetDefinition)
    {
        return await Task.Run(() =>
        {
            try
            {
                return widgetDefinition.Id;
            }
            catch (Exception ex)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
            }

            return string.Empty;
        });
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _getDefinitionLock.Dispose();
            }

            _disposedValue = true;
        }
    }
}
