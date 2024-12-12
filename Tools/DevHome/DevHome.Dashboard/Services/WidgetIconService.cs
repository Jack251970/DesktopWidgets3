// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.Storage.Streams;

namespace DevHome.Dashboard.Services;

public class WidgetIconService(DispatcherQueue dispatcherQueue, IWidgetResourceService widgetResourceService) : IWidgetIconService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetIconService));

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetLightIconCache = new();
    private readonly ConcurrentDictionary<(string, string), BitmapImage> _desktopWidgets3WidgetDarkIconCache = new();

    private readonly ConcurrentDictionary<string, BitmapImage> _microsoftWidgetLightIconCache = new();
    private readonly ConcurrentDictionary<string, BitmapImage> _microsoftWidgetDarkIconCache = new();

    public void RemoveIconsFromDesktopWidgets3Cache(string widgetId, string widgetType)
    {
        _desktopWidgets3WidgetLightIconCache.TryRemove((widgetId, widgetType), out _);
        _desktopWidgets3WidgetDarkIconCache.TryRemove((widgetId, widgetType), out _);
    }

    private async Task<BitmapImage> GetIconFromDesktopWidgets3CacheAsync(string widgetId, string widgetType, ElementTheme actualTheme)
    {
        BitmapImage bitmapImage;

        // First, check the cache to see if the icon is already there.
        if (actualTheme == ElementTheme.Dark)
        {
            _desktopWidgets3WidgetDarkIconCache.TryGetValue((widgetId, widgetType), out bitmapImage);
        }
        else
        {
            _desktopWidgets3WidgetLightIconCache.TryGetValue((widgetId, widgetType), out bitmapImage);
        }

        if (bitmapImage != null)
        {
            return bitmapImage;
        }

        // If the icon wasn't already in the cache, get it from the widget definition and add it to the cache before returning.
        if (actualTheme == ElementTheme.Dark)
        {
            bitmapImage = await DesktopWidgets3WidgetIconToBitmapImageAsync(_widgetResourceService.GetWidgetIconPath(widgetId, widgetType, ElementTheme.Dark));
            _desktopWidgets3WidgetDarkIconCache.TryAdd((widgetId, widgetType), bitmapImage);
        }
        else
        {
            bitmapImage = await DesktopWidgets3WidgetIconToBitmapImageAsync(_widgetResourceService.GetWidgetIconPath(widgetId, widgetType, ElementTheme.Dark));
            _desktopWidgets3WidgetLightIconCache.TryAdd((widgetId, widgetType), bitmapImage);
        }

        return bitmapImage;
    }

    public async Task<Brush> GetBrushForDesktopWidgets3WidgetIconAsync(string widgetId, string widgetType, ElementTheme actualTheme)
    {
        var image = new BitmapImage();
        try
        {
            image = await GetIconFromDesktopWidgets3CacheAsync(widgetId, widgetType, actualTheme);
        }
        catch (System.IO.FileNotFoundException fileNotFoundEx)
        {
            _log.Warning(fileNotFoundEx, $"Widget icon missing for widget {widgetId} - {widgetType}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get widget icon for widget {widgetId} - {widgetType}");
        }

        var brush = new ImageBrush
        {
            ImageSource = image,
            Stretch = Stretch.Uniform,
        };

        return brush;
    }

    public void RemoveIconsFromMicrosoftCache(string definitionId)
    {
        _microsoftWidgetLightIconCache.TryRemove(definitionId, out _);
        _microsoftWidgetDarkIconCache.TryRemove(definitionId, out _);
    }

    private async Task<BitmapImage> GetIconFromMicrosoftCacheAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme)
    {
        var widgetDefinitionId = widgetDefinition.Id;
        BitmapImage bitmapImage;

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
            bitmapImage = await MicrosoftWidgetIconToBitmapImageAsync((await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Dark)).Icon);
            _microsoftWidgetDarkIconCache.TryAdd(widgetDefinitionId, bitmapImage);
        }
        else
        {
            bitmapImage = await MicrosoftWidgetIconToBitmapImageAsync((await widgetDefinition.GetThemeResourceAsync(WidgetTheme.Light)).Icon);
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
        catch (System.IO.FileNotFoundException fileNotFoundEx)
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

    private async Task<BitmapImage> DesktopWidgets3WidgetIconToBitmapImageAsync(string iconPath)
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

    private async Task<BitmapImage> MicrosoftWidgetIconToBitmapImageAsync(IRandomAccessStreamReference iconStreamRef)
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
