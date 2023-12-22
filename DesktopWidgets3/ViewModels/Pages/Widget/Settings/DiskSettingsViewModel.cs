using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

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
