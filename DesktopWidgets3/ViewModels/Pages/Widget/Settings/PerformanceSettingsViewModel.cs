using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

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
