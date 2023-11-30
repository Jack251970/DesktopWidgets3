using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Views.Pages.Widget;

public sealed partial class FrameShellPage : Page
{
    public FrameShellViewModel ViewModel
    {
        get;
    }

    public WidgetType WidgetType
    {
        get;
    }

    public FrameShellPage(FrameShellViewModel viewModel, IWidgetManagerService widgetManagerService)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.WidgetNavigationService.Frame = NavigationFrame;

        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        var window = widgetManagerService.GetWidgetWindow();
        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(AppTitleBar);

        WidgetType = widgetManagerService.GetWidgetType();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        ViewModel.WidgetNavigationService.NavigateTo(WidgetType);
    }
}
