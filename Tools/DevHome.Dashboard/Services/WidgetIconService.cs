// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DevHome.Dashboard.Services;

public class WidgetIconService(DispatcherQueue dispatcherQueue) : IWidgetIconService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetIconService));

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;

    #region Widget Icon

    private readonly ConcurrentDictionary<string, BitmapImage> _microsoftWidgetLightIconCache = new();
    private readonly ConcurrentDictionary<string, BitmapImage> _microsoftWidgetDarkIconCache = new();

    public void RemoveIconsFromMicrosoftIconCache(string definitionId)
    {
        _microsoftWidgetLightIconCache.TryRemove(definitionId, out _);
        _microsoftWidgetDarkIconCache.TryRemove(definitionId, out _);
    }

    private async Task<BitmapImage> GetIconFromMicrosoftCacheAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        var widgetDefinitionId = widgetDefinition.Id;
        BitmapImage? bitmapImage;

        // First, check the cache to see if the icon is already there.
        if (actualTheme == ElementTheme.Dark)
        {
            _microsoftWidgetDarkIconCache.TryGetValue(widgetDefinitionId, out bitmapImage);
        }
        else
        {
            _microsoftWidgetLightIconCache.TryGetValue(widgetDefinitionId, out bitmapImage);
        }

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the icon wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
        if (actualTheme == ElementTheme.Dark)
        {
            bitmapImage = await BitmapImageHelper.RandomAccessStreamToBitmapImageAsync(_dispatcherQueue, (await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Dark)).Icon);
            _microsoftWidgetDarkIconCache.TryAdd(widgetDefinitionId, bitmapImage);
        }
        else
        {
            bitmapImage = await BitmapImageHelper.RandomAccessStreamToBitmapImageAsync(_dispatcherQueue, (await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Light)).Icon);
            _microsoftWidgetLightIconCache.TryAdd(widgetDefinitionId, bitmapImage);
        }

        return bitmapImage;
    }

    public async Task<Brush> GetBrushForMicrosoftWidgetIconAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetIconFromMicrosoftCacheAsync(widgetDefinition, actualTheme);
        }
        catch (FileNotFoundException fileNotFoundEx)
        {
            _log.Warning(fileNotFoundEx, $"Widget icon missing for widget definition {widgetDefinition.DisplayTitle}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get widget icon for widget definition {widgetDefinition.DisplayTitle}");
        }

        var brush = new ImageBrush
        {
            ImageSource = image,
            Stretch = Stretch.Uniform,
        };

        return brush;
    }

    #endregion

    #region Widget Group Icon

    private readonly ConcurrentDictionary<string, BitmapImage> _microsoftWidgetProviderIconCache = new();

    public void RemoveIconsFromMicrosoftProviderIconCache(string providerDefinitionId)
    {
        _microsoftWidgetProviderIconCache.TryRemove(providerDefinitionId, out _);
    }

    private async Task<BitmapImage> GetIconFromMicrosoftProviderCacheAsync(WidgetProviderDefinition widgetProviderDefinition)
    {
        var widgetDefinitionId = widgetProviderDefinition.Id;
        BitmapImage? bitmapImage;

        // First, check the cache to see if the icon is already there.
        _microsoftWidgetProviderIconCache.TryGetValue(widgetDefinitionId, out bitmapImage);

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the icon wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
        bitmapImage = await BitmapImageHelper.RandomAccessStreamToBitmapImageAsync(_dispatcherQueue, widgetProviderDefinition.Icon);
        _microsoftWidgetProviderIconCache.TryAdd(widgetDefinitionId, bitmapImage);

        return bitmapImage;
    }

    public async Task<Brush> GetBrushForMicrosoftWidgetProviderIconAsync(WidgetProviderDefinition widgetProviderDefinition)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetIconFromMicrosoftProviderCacheAsync(widgetProviderDefinition);
        }
        catch (FileNotFoundException fileNotFoundEx)
        {
            _log.Warning(fileNotFoundEx, $"Widget icon missing for widget provider definition {widgetProviderDefinition.DisplayName}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get widget icon for widget provider definition {widgetProviderDefinition.DisplayName}");
        }

        var brush = new ImageBrush
        {
            ImageSource = image,
            Stretch = Stretch.Uniform,
        };

        return brush;
    }

    #endregion
}
