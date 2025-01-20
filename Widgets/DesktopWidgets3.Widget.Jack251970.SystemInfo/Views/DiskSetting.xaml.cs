using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

public sealed partial class DiskSetting : UserControl, IWidgetSettingViewBase
{
    public DiskSettingViewModel ViewModel;

    public bool IsNavigated { get; private set; }

    public DiskSetting(string widgetId)
    {
        ViewModel = new DiskSettingViewModel(widgetId);
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
