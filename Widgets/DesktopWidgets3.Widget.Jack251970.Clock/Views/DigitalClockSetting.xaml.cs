using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Clock.Views;

public sealed partial class DigitalClockSetting : UserControl, IWidgetSettingViewBase
{
    public DigitalClockSettingViewModel ViewModel;

    public bool IsNavigated { get; private set; }

    public DigitalClockSetting(string widgetId, ResourceDictionary? resourceDictionary)
    {
        ViewModel = new DigitalClockSettingViewModel(widgetId);
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
