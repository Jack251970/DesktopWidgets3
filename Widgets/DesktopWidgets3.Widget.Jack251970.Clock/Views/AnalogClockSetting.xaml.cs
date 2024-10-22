using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Clock.Views;

public sealed partial class AnalogClockSetting : UserControl, IWidgetSettingViewBase
{
    public AnalogClockSettingViewModel ViewModel;

    public bool IsNavigated { get; private set; }

    public AnalogClockSetting(string widgetId, ResourceDictionary? resourceDictionary)
    {
        ViewModel = new AnalogClockSettingViewModel(widgetId);
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
