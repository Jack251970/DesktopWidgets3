using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3.Views.Pages.Widget;

public sealed partial class FrameShellPage : Page
{
    public FrameShellViewModel ViewModel
    {
        get;
    }

    public BlankWindow WidgetWindow
    {
        get;
    }

    private readonly IWidgetManagerService _widgetManagerService;

    public FrameShellPage(FrameShellViewModel viewModel, IWidgetManagerService widgetManagerService)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.WidgetNavigationService.Frame = NavigationFrame;
        WidgetWindow = widgetManagerService.GetCurrentWidgetWindow();

        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        WidgetWindow.ExtendsContentIntoTitleBar = true;
        WidgetWindow.SetTitleBar(AppTitleBar);
        WidgetWindow.InitializeTitleBar(AppTitleBar);

        // Load registered services
        _widgetManagerService = widgetManagerService;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        ViewModel.WidgetNavigationService.NavigateTo(WidgetWindow.WidgetType);
    }
}
