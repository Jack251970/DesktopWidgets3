using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetPageService : IWidgetPageService
{
    private readonly Dictionary<WidgetType, Type> _pages = [];

    public WidgetPageService()
    {
        Configure<ClockPage>(WidgetType.Clock);
        Configure<PerformancePage>(WidgetType.Performance);
        Configure<DiskPage>(WidgetType.Disk);
        Configure<FolderViewPage>(WidgetType.FolderView);
        Configure<NetworkPage>(WidgetType.Network);
    }

    public Type GetPageType(WidgetType widgetType)
    {
        Type? page;
        lock (_pages)
        {
            if (!_pages.TryGetValue(widgetType, out page))
            {
                throw new ArgumentException($"Page not found: {widgetType}. Did you forget to call WidgetPageService.Configure?");
            }
        }

        return page;
    }

    private void Configure<V>(WidgetType widgetType)
        where V : Page
    {
        lock (_pages)
        {
            if (_pages.ContainsKey(widgetType))
            {
                throw new ArgumentException($"The key {widgetType} is already configured in WidgetPageService!");
            }

            var view = typeof(V);
            if (_pages.ContainsValue(view))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == view).Key}!");
            }

            _pages.Add(widgetType, view);
        }
    }
}

