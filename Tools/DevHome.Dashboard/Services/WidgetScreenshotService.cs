﻿// Copyright (c) Microsoft Corporation.
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

public class WidgetScreenshotService(DispatcherQueue dispatcherQueue) : IWidgetScreenshotService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetScreenshotService));

    private readonly ConcurrentDictionary<string, BitmapImage> _microsoftWidgetLightScreenshotCache = new();
    private readonly ConcurrentDictionary<string, BitmapImage> _microsoftWidgetDarkScreenshotCache = new();

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;

    public void RemoveScreenshotsFromMicrosoftIconCache(string definitionId)
    {
        _microsoftWidgetLightScreenshotCache.Remove(definitionId, out _);
        _microsoftWidgetDarkScreenshotCache.Remove(definitionId, out _);
    }

    private async Task<BitmapImage> GetScreenshotFromMicrosoftCacheAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        var widgetDefinitionId = widgetDefinition.Id;
        BitmapImage? bitmapImage;

        // First, check the cache to see if the screenshot is already there.
        if (actualTheme == ElementTheme.Dark)
        {
            _microsoftWidgetDarkScreenshotCache.TryGetValue(widgetDefinitionId, out bitmapImage);
        }
        else
        {
            _microsoftWidgetLightScreenshotCache.TryGetValue(widgetDefinitionId, out bitmapImage);
        }

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the screenshot wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
        if (actualTheme == ElementTheme.Dark)
        {
            bitmapImage = await BitmapImageHelper.RandomAccessStreamToBitmapImageAsync(_dispatcherQueue, (await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Dark)).GetScreenshots().FirstOrDefault()!.Image);
            _microsoftWidgetDarkScreenshotCache.TryAdd(widgetDefinitionId, bitmapImage);
        }
        else
        {
            bitmapImage = await BitmapImageHelper.RandomAccessStreamToBitmapImageAsync(_dispatcherQueue, (await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Light)).GetScreenshots().FirstOrDefault()!.Image);
            _microsoftWidgetLightScreenshotCache.TryAdd(widgetDefinitionId, bitmapImage);
        }

        return bitmapImage;
    }

    public async Task<Brush> GetBrushForMicrosoftWidgetScreenshotAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetScreenshotFromMicrosoftCacheAsync(widgetDefinition, actualTheme);
        }
        catch (FileNotFoundException fileNotFoundEx)
        {
            _log.Warning(fileNotFoundEx, $"Widget screenshot missing for widget definition {widgetDefinition.DisplayTitle}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get widget screenshot for widget definition {widgetDefinition.DisplayTitle}");
        }

        var brush = new ImageBrush
        {
            ImageSource = image,
            Stretch = Stretch.Uniform
        };

        return brush;
    }
}
