using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Clock.Views;

public sealed partial class DigitalClockWidget : UserControl, IWidgetViewBase
{
    public const string Type = "Clock_DigitalClock";

    public DigitalClockViewModel ViewModel;

    public bool IsActivated { get; private set; } = false;

    public DigitalClockWidget(string widgetId)
    {
        ViewModel = new DigitalClockViewModel(widgetId);
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
