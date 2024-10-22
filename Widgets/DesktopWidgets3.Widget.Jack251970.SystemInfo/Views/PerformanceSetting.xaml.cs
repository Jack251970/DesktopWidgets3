using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

public sealed partial class PerformanceSetting : UserControl, IWidgetSettingViewBase
{
    public PerformanceSettingViewModel ViewModel;

    public bool IsNavigated { get; private set; }

    public PerformanceSetting(string widgetId, ResourceDictionary? resourceDictionary)
    {
        ViewModel = new PerformanceSettingViewModel(widgetId);
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
