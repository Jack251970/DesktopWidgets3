namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public partial class PerformanceSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region view properties

    #endregion

    private readonly IWidgetManagerService _widgetManagerService;

    private PerformanceWidgetSettings Settings => (PerformanceWidgetSettings)WidgetSettings!;

    public PerformanceSettingsViewModel(IWidgetManagerService widgetManagerService)
    {
        _widgetManagerService = widgetManagerService;
    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Performance;

    protected override void InitializeWidgetSettings()
    {
        
    }
}
