using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Clock.Views;

public sealed partial class AnalogClockWidget : UserControl, IWidgetViewBase
{
    public const string Type = "Clock_AnalogClock";

    public AnalogClockViewModel ViewModel;

    public bool IsActivated { get; private set; } = false;

    public AnalogClockWidget(string widgetId, ResourceDictionary? resourceDictionary)
    {
        ViewModel = new AnalogClockViewModel(widgetId);
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }

    public void Activate(IWidgetContext widgetContext)
    {
        IsActivated = true;
        ViewModel.StartAllTimers();
    }

    public void Deactivate(string widgetId)
    {
        IsActivated = false;
        ViewModel.StopAllTimers();
    }

    public void OnWidgetSettingsChanged(WidgetSettingsChangedArgs contextChangedArgs)
    {
        var widgetSettings = contextChangedArgs.Settings;
        ViewModel.LoadSettings(widgetSettings);
    }

    public void Dispose()
    {
        ViewModel.DisposeAllTimers();
    }
}
