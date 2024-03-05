namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public partial class DiskSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region view properties

    #endregion

    private DiskWidgetSettings Settings => (DiskWidgetSettings)WidgetSettings!;

    public DiskSettingsViewModel()
    {

    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Disk;

    protected override void InitializeWidgetSettings()
    {

    }
}
