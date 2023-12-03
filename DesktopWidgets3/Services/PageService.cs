using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.ViewModels.Pages;
using DesktopWidgets3.Views.Pages;
using DesktopWidgets3.ViewModels.Pages.Widget.Settings;
using DesktopWidgets3.Views.Pages.Widget.Settings;

namespace DesktopWidgets3.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new();

    public PageService()
    {
        Configure<HomeViewModel, HomePage>();
        Configure<WidgetSettingViewModel, WidgetSettingPage>();
        Configure<SettingsViewModel, SettingsPage>();
        Configure<DashboardViewModel, DashboardPage>();
        Configure<ClockSettingsViewModel, ClockSettingsPage>();
        Configure<FolderViewSettingsViewModel, FolderViewSettingsPage>();
    }

    public Type GetPageType(string viewModel)
    {
        Type? page;
        lock (_pages)
        {
            if (!_pages.TryGetValue(viewModel, out page))
            {
                throw new ArgumentException($"Page not found: {viewModel}. Did you forget to call PageService.Configure?");
            }
        }

        return page;
    }

    public string GetPageKey(Type pageType)
    {
        lock (_pages)
        {
            if (!_pages.ContainsValue(pageType))
            {
                throw new ArgumentException($"Page not found: {pageType}. Did you forget to call PageService.Configure?");
            }

            return _pages.FirstOrDefault(p => p.Value == pageType).Key;
        }
    }

    private void Configure<VM, V>()
        where VM : ObservableObject
        where V : Page
    {
        lock (_pages)
        {
            var viewModel = typeof(VM).FullName!;
            if (_pages.ContainsKey(viewModel))
            {
                throw new ArgumentException($"The key {viewModel} is already configured in PageService!");
            }

            var view = typeof(V);
            if (_pages.ContainsValue(view))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == view).Key}!");
            }

            _pages.Add(viewModel, view);
        }
    }
}
