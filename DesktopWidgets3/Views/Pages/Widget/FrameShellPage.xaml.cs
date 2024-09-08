using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets;

public sealed partial class FrameShellPage : Page
{
    public FrameShellViewModel ViewModel { get; }

    public FrameworkElement? FrameworkElement => WidgetScrollViewer.Content as FrameworkElement;

    private WidgetWindow WidgetWindow { get; set; } = null!;

    public FrameShellPage(FrameShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
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

    public void SetFrameworkElement(FrameworkElement frameworkElement)
    {
        ViewModel.WidgetFrameworkElement = frameworkElement;
    }
}
