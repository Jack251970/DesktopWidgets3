using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets;

public sealed partial class FrameShellPage : Page
{
    public FrameShellViewModel ViewModel { get; }

    public Frame NavigationFrame { get; }

    private WidgetWindow WidgetWindow { get; set; } = null!;

    public FrameShellPage(FrameShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        NavigationFrame = WidgetNavigationFrame;
        ViewModel.WidgetNavigationService.Frame = WidgetNavigationFrame;
    }

    public void InitializeWindow(WidgetWindow window)
    {
        WidgetWindow = window;
        SetCustomTitleBar(false);
    }

    public void SetCustomTitleBar(bool customTitleBar)
    {
        WidgetWindow.ExtendsContentIntoTitleBar = customTitleBar;
        WidgetWindow.SetTitleBar(customTitleBar ? WidgetTitleBar : null);
        WidgetWindow.InitializeTitleBar();
    }
}
