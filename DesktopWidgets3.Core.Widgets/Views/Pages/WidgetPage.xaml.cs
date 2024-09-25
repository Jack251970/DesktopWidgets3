using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace DesktopWidgets3.Core.Widgets.Views.Pages;

public sealed partial class WidgetPage : Page
{
    public WidgetViewModel ViewModel { get; }

    private WidgetWindow WidgetWindow { get; set; } = null!;

    private readonly IWidgetResourceService _widgetResourceService = DependencyExtensions.GetRequiredService<IWidgetResourceService>();

    public WidgetPage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<WidgetViewModel>();
        InitializeComponent();
    }

    #region Initialization

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

    #endregion

    #region Widget Menu

    private async void OpenWidgetMenuAsync(object sender, RoutedEventArgs e)
    {
        if (sender as Button is Button widgetMenuButton && widgetMenuButton.Flyout is MenuFlyout widgetMenuFlyout)
        {
            widgetMenuFlyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
            if (widgetMenuFlyout?.Items.Count == 0)
            {
                /*await AddSizesToWidgetMenuAsync(widgetMenuFlyout, widgetViewModel);
                widgetMenuFlyout.Items.Add(new MenuFlyoutSeparator());
                AddCustomizeToWidgetMenu(widgetMenuFlyout, widgetViewModel);
                AddRemoveToWidgetMenu(widgetMenuFlyout, widgetViewModel);*/
            }
        }
    }

    #endregion
}
