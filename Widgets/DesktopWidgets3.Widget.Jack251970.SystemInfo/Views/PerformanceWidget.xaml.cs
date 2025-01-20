using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

public sealed partial class PerformanceWidget : UserControl, IWidgetViewBase
{
    public const string Type = "SystemInfo_Performance";

    public PerformanceViewModel ViewModel;

    public bool IsActivated { get; private set; } = false;

    public PerformanceWidget(string widgetId, HardwareInfoService hardwareInfoService)
    {
        ViewModel = new PerformanceViewModel(widgetId, hardwareInfoService);
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
