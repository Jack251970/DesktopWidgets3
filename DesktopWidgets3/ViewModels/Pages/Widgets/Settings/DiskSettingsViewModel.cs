namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public partial class DiskSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region view properties

    #endregion

    private readonly IWidgetManagerService _widgetManagerService;

    private DiskWidgetSettings Settings => (DiskWidgetSettings)WidgetSettings!;

    public DiskSettingsViewModel(IWidgetManagerService widgetManagerService)
    {
        _widgetManagerService = widgetManagerService;
    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Disk;

    protected override void InitializeWidgetSettings()
    {

    }
}
