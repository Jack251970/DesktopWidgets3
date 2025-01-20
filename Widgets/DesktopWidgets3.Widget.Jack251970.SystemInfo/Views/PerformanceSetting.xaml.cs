using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

public sealed partial class PerformanceSetting : UserControl, IWidgetSettingViewBase
{
    public PerformanceSettingViewModel ViewModel;

    public bool IsNavigated { get; private set; }

    public PerformanceSetting(string widgetId)
    {
        ViewModel = new PerformanceSettingViewModel(widgetId);
        InitializeComponent();
    }

    public void OnWidgetSettingsChanged(WidgetSettingsChangedArgs contextChangedArgs)
    {
        var widgetSettings = contextChangedArgs.Settings;
        ViewModel.LoadSettings(widgetSettings);
    }

    public void Dispose()
    {

    }
}
