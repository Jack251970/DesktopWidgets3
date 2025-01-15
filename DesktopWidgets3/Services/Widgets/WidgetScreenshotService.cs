// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.Storage.Streams;

namespace DesktopWidgets3.Services.Widgets;

public class WidgetScreenshotService(DispatcherQueue dispatcherQueue, IWidgetResourceService widgetResourceService) : IWidgetScreenshotService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetScreenshotService));

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetLightScreenshotCache = new();
    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetDarkScreenshotCache = new();

    private readonly ConcurrentDictionary<string, BitmapImage> _microsoftWidgetLightScreenshotCache = new();
    private readonly ConcurrentDictionary<string, BitmapImage> _microsoftWidgetDarkScreenshotCache = new();

    public void RemoveScreenshotsFromDesktopWidgets3Cache(string widgetId, string widgetType)
    {
        _desktopWidgets3WidgetLightScreenshotCache.Remove((widgetId, widgetType), out _);
        _desktopWidgets3WidgetDarkScreenshotCache.Remove((widgetId, widgetType), out _);
    }

    private async Task<BitmapImage> GetScreenshotFromDesktopWidgets3CacheAsync(string widgetId, string widgetType, ElementTheme actualTheme)
    {
        BitmapImage? bitmapImage;

        // First, check the cache to see if the screenshot is already there.
        if (actualTheme == ElementTheme.Dark)
        {
            _desktopWidgets3WidgetDarkScreenshotCache.TryGetValue((widgetId, widgetType), out bitmapImage);
        }
        else
        {
            _desktopWidgets3WidgetLightScreenshotCache.TryGetValue((widgetId, widgetType), out bitmapImage);
        }

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the screenshot wasn't already in the cache, get it from the widget resources service and add it to the cache before returning.
        if (actualTheme == ElementTheme.Dark)
        {
            bitmapImage = await DesktopWidgets3WidgetScreenshotToBitmapImageAsync(_widgetResourceService.GetWidgetScreenshotPath(widgetId, widgetType, ElementTheme.Dark));
            _desktopWidgets3WidgetDarkScreenshotCache.TryAdd((widgetId, widgetType), bitmapImage);
        }
        else
        {
            bitmapImage = await DesktopWidgets3WidgetScreenshotToBitmapImageAsync(_widgetResourceService.GetWidgetScreenshotPath(widgetId, widgetType, ElementTheme.Light));
            _desktopWidgets3WidgetLightScreenshotCache.TryAdd((widgetId, widgetType), bitmapImage);
        }

        return bitmapImage;
    }

    public async Task<Brush> GetBrushForDesktopWidgets3WidgetScreenshotAsync(string widgetId, string widgetType, ElementTheme actualTheme)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetScreenshotFromDesktopWidgets3CacheAsync(widgetId, widgetType, actualTheme);
        }
        catch (FileNotFoundException fileNotFoundEx)
        {
            _log.Warning(fileNotFoundEx, $"Widget screenshot missing for widget definition {widgetId} {widgetType}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get widget screenshot for widget definition {widgetId} {widgetType}");
        }

        var brush = new ImageBrush
        {
            ImageSource = image,
        };

        return brush;
    }

    public void RemoveScreenshotsFromMicrosoftCache(string definitionId)
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
            bitmapImage = await MicrosoftWidgetScreenshotToBitmapImageAsync((await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Dark)).GetScreenshots().FirstOrDefault()!.Image);
            _microsoftWidgetDarkScreenshotCache.TryAdd(widgetDefinitionId, bitmapImage);
        }
        else
        {
            bitmapImage = await MicrosoftWidgetScreenshotToBitmapImageAsync((await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Light)).GetScreenshots().FirstOrDefault()!.Image);
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
        };

        return brush;
    }

    private async Task<BitmapImage> DesktopWidgets3WidgetScreenshotToBitmapImageAsync(string iconPath)
    {
        // Return the bitmap image via TaskCompletionSource. Using WCT's EnqueueAsync does not suffice here, since if
        // we're already on the thread of the DispatcherQueue then it just directly calls the function, with no async involved.
        var completionSource = new TaskCompletionSource<BitmapImage>();
        _dispatcherQueue.TryEnqueue(() =>
        {
            var itemImage = new BitmapImage
            {
                UriSource = new Uri(iconPath)
            };
            completionSource.TrySetResult(itemImage);
        });

        var bitmapImage = await completionSource.Task;

        return bitmapImage;
    }

    private async Task<BitmapImage> MicrosoftWidgetScreenshotToBitmapImageAsync(IRandomAccessStreamReference iconStreamRef)
    {
        // Return the bitmap image via TaskCompletionSource. Using WCT's EnqueueAsync does not suffice here, since if
        // we're already on the thread of the DispatcherQueue then it just directly calls the function, with no async involved.
        var completionSource = new TaskCompletionSource<BitmapImage>();
        _dispatcherQueue.TryEnqueue(async () =>
        {
            using var bitmapStream = await iconStreamRef.OpenReadAsync();
            var itemImage = new BitmapImage();
            await itemImage.SetSourceAsync(bitmapStream);
            completionSource.TrySetResult(itemImage);
        });

        var bitmapImage = await completionSource.Task;

        return bitmapImage;
    }
}
