namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public partial class PerformanceSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region view properties

    #endregion

    private PerformanceWidgetSettings Settings => (PerformanceWidgetSettings)WidgetSettings!;

    public PerformanceSettingsViewModel()
    {

    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Performance;

    protected override void InitializeWidgetSettings()
    {
        
    }
}
