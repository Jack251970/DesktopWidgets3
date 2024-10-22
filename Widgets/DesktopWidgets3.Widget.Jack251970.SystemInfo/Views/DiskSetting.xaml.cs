using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

public sealed partial class DiskSetting : UserControl, IWidgetSettingViewBase
{
    public DiskSettingViewModel ViewModel;

    public bool IsNavigated { get; private set; }

    public DiskSetting(string widgetId, ResourceDictionary? resourceDictionary)
    {
        ViewModel = new DiskSettingViewModel(widgetId);
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
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
