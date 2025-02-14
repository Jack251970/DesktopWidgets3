﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DevHome.Dashboard.Services;

public class WidgetHostingService : IWidgetHostingService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetHostingService));

    private WidgetHost _widgetHost = null!;
    private WidgetCatalog _widgetCatalog = null!;

    // RPC error codes to recover from
    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    private const int RpcCallFailed = unchecked((int)0x800706BE);

    private const int MaxAttempts = 3;

    /// <inheritdoc />
    public async Task<Widget[]> GetWidgetsAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetHost ??= await Task.Run(() => WidgetHost.Register(new WidgetHostContext(Constants.MicrosoftWidgetHostId)));
                return await Task.Run(() => _widgetHost.GetWidgets()) ?? [];
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetHost before trying again. Also reset the WidgetCatalog,
                // since if we lost the host we probably lost the catalog too.
                _widgetHost = null!;
                _widgetCatalog = null!;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception getting widgets from service:");
            }
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<Widget> GetWidgetAsync(string widgetId)
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetHost ??= await Task.Run(() => WidgetHost.Register(new WidgetHostContext(Constants.MicrosoftWidgetHostId)));
                return await Task.Run(() => _widgetHost.GetWidget(widgetId));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                _widgetHost = null!;
                _widgetCatalog = null!;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Exception getting widget with id {widgetId}:");
            }
        }

        return null!;
    }

    /// <inheritdoc />
    public async Task<Widget> CreateWidgetAsync(string widgetDefinitionId, WidgetSize widgetSize)
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetHost ??= await Task.Run(() => WidgetHost.Register(new WidgetHostContext(Constants.MicrosoftWidgetHostId)));
                return await Task.Run(async () => await _widgetHost.CreateWidgetAsync(widgetDefinitionId, widgetSize));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                _widgetHost = null!;
                _widgetCatalog = null!;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception creating a widget:");
            }
        }

        return null!;
    }

    /// <inheritdoc />
    public async Task<WidgetCatalog> GetWidgetCatalogAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(WidgetCatalog.GetDefault);

                // Need to use an arbitrary method to check if the COM object is still alive.
                await Task.Run(() => _widgetCatalog.GetWidgetDefinition("fakeWidgetDefinitionId"));

                // If the above call didn't throw, the object is still alive.
                return _widgetCatalog;
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                _widgetHost = null!;
                _widgetCatalog = null!;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetCatalogAsync:");
                _widgetCatalog = null!;
            }
        }

        return _widgetCatalog;
    }

    /// <inheritdoc />
    public async Task<WidgetProviderDefinition[]> GetProviderDefinitionsAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(WidgetCatalog.GetDefault);
                return await Task.Run(() => _widgetCatalog.GetProviderDefinitions());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetCatalog before trying again. Also reset the WidgetHost,
                // since if we lost the catalog we probably lost the host too.
                _widgetHost = null!;
                _widgetCatalog = null!;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetProviderDefinitionsAsync:");
            }
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<WidgetDefinition[]> GetWidgetDefinitionsAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(WidgetCatalog.GetDefault);
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinitions());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetCatalog before trying again. Also reset the WidgetHost,
                // since if we lost the catalog we probably lost the host too.
                _widgetHost = null!;
                _widgetCatalog = null!;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionsAsync:");
            }
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<WidgetDefinition> GetWidgetDefinitionAsync(string widgetDefinitionId)
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(WidgetCatalog.GetDefault);
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinition(widgetDefinitionId));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetCatalog before trying again. Also reset the WidgetHost,
                // since if we lost the catalog we probably lost the host too.
                _widgetHost = null!;
                _widgetCatalog = null!;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
            }
        }

        return null!;
    }
}
