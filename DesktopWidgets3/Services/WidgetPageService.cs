using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Pages.Widget.Clock;
using DesktopWidgets3.Views.Pages.Widget.FolderView;
using DesktopWidgets3.Views.Pages.Widget.CPU;
using DesktopWidgets3.Views.Pages.Widget.Disk;
using DesktopWidgets3.Views.Pages.Widget.Network;

namespace DesktopWidgets3.Services;

public class WidgetPageService : IWidgetPageService
{
    private readonly Dictionary<WidgetType, Type> _pages = new();

    public WidgetPageService()
    {
        Configure<ClockPage>(WidgetType.Clock);
        Configure<CPUPage>(WidgetType.CPU);
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

