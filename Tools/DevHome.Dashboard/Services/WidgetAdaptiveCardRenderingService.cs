﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using DevHome.Dashboard.Common.Renderers;
using DevHome.Dashboard.Common.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Storage;

namespace DevHome.Dashboard.Services;

public partial class WidgetAdaptiveCardRenderingService : IAdaptiveCardRenderingService, IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetAdaptiveCardRenderingService));

    public event EventHandler RendererUpdated = (_, _) => { };

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly IThemeSelectorService _themeSelectorService;

    private readonly SemaphoreSlim _rendererLock = new(1, 1);

    private AdaptiveCardRenderer _renderer = null!;

    private bool _disposedValue;

    public WidgetAdaptiveCardRenderingService(DispatcherQueue dispatcherQueue, IThemeSelectorService themeSelectorService)
    {
        _dispatcherQueue = dispatcherQueue;
        _themeSelectorService = themeSelectorService;
        _themeSelectorService.ThemeChanged += OnThemeChanged;
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
                _rendererLock.Dispose();
            }

            _disposedValue = true;
        }
    }

    public async Task<AdaptiveCardRenderer> GetRendererAsync()
    {
        // We need to lock the renderer, otherwise another widget could come in after the renderer
        // is created but before it is configured and render the widget without configuration.
        await _rendererLock.WaitAsync();
        try
        {
            if (_renderer == null)
            {
                _renderer = new AdaptiveCardRenderer();
                await ConfigureAdaptiveCardRendererAsync();
            }

            return _renderer;
        }
        finally
        {
            _rendererLock.Release();
        }
    }

    private async Task ConfigureAdaptiveCardRendererAsync()
    {
        // Add custom Adaptive Card renderer.
        _renderer.ElementRenderers.Set(LabelGroup.CustomTypeString, new LabelGroupRenderer());
        _renderer.ElementRenderers.Set("Input.ChoiceSet", new AccessibleChoiceSet());
        _renderer.ElementRenderers.Set("Input.Text", new TextInputRenderer());
        _renderer.ActionRenderers.Set(ChooseFileAction.CustomTypeString, new ChooseFileActionRenderer());

        // A different host config is used to render widgets (adaptive cards) in light and dark themes.
        await UpdateHostConfig();

        // Due to a bug in the Adaptive Card renderer (https://github.com/microsoft/AdaptiveCards/issues/8840)
        // positive and destructive buttons render with square corners. Override with XAML styles.
        var positiveStyle = Application.Current.Resources["AccentButtonStyle"] as Style;
        var destructiveStyle = Application.Current.Resources["DefaultButtonStyle"] as Style;

        _renderer.OverrideStyles = new ResourceDictionary
        {
            { "Adaptive.Action.Positive", positiveStyle },
            { "Adaptive.Action.Destructive", destructiveStyle },
        };
    }

    private async Task UpdateHostConfig()
    {
        if (_renderer != null)
        {
            // Add host config for current theme.
            var hostConfigContents = string.Empty;
            var hostConfigFileName = _themeSelectorService.IsDarkTheme() ? "HostConfigDark.json" : "HostConfigLight.json";
            try
            {
                _log.Information($"Get HostConfig file '{hostConfigFileName}'");
                StorageFile file;
                if (RuntimeHelper.IsMSIX)
                {
                    var uri = new Uri($"ms-appx:///DevHome.Dashboard/Assets/{hostConfigFileName}");
                    file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
                }
                else
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "DevHome.Dashboard", "Assets", hostConfigFileName);
                    file = await StorageFile.GetFileFromPathAsync(path).AsTask().ConfigureAwait(false);
                }
                hostConfigContents = await FileIO.ReadTextAsync(file);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error retrieving HostConfig");
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (!string.IsNullOrEmpty(hostConfigContents))
                {
                    _renderer.HostConfig = AdaptiveHostConfig.FromJsonString(hostConfigContents).HostConfig;

                    // Remove margins from selectAction.
                    _renderer.AddSelectActionMargin = false;
                }
                else
                {
                    _log.Error($"HostConfig contents are {hostConfigContents}");
                }
            });

            RendererUpdated(this, null!);
        }
    }

    private async void OnThemeChanged(object? sender, ElementTheme e) => await UpdateHostConfig();
}
