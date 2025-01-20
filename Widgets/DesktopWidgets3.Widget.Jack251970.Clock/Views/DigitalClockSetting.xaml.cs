using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Clock.Views;

public sealed partial class DigitalClockSetting : UserControl, IWidgetSettingViewBase
{
    public DigitalClockSettingViewModel ViewModel;

    public bool IsNavigated { get; private set; }

    public DigitalClockSetting(string widgetId)
    {
        ViewModel = new DigitalClockSettingViewModel(widgetId);
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
