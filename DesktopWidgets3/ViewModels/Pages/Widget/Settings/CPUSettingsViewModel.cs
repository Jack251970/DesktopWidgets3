using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

public partial class CPUSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region observable properties

    #endregion

    private readonly IWidgetManagerService _widgetManagerService;

    private CPUWidgetSettings Settings => (CPUWidgetSettings)WidgetSettings!;

    public CPUSettingsViewModel(IWidgetManagerService widgetManagerService)
    {
        _widgetManagerService = widgetManagerService;
    }

    protected override WidgetType InitializeWidgetType() => WidgetType.CPU;

    protected override void InitializeWidgetSettings()
    {
        
    }
}
