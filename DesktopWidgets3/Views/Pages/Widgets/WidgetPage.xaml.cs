using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets;

public sealed partial class WidgetPage : Page
{
    public WidgetViewModel ViewModel { get; }

    private WidgetWindow WidgetWindow { get; set; } = null!;

    public WidgetPage()
    {
        ViewModel = App.GetService<WidgetViewModel>();
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
}
