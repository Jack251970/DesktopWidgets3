using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

public sealed partial class NetworkSetting : UserControl, IWidgetSettingViewBase
{
    public NetworkSettingViewModel ViewModel;

    public bool IsNavigated { get; private set; }

    public NetworkSetting(string widgetId)
    {
        ViewModel = new NetworkSettingViewModel(widgetId);
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
