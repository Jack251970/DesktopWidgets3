using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

public partial class NetworkSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region observable properties

    #endregion

    private readonly IWidgetManagerService _widgetManagerService;

    private NetworkWidgetSettings Settings => (NetworkWidgetSettings)WidgetSettings!;

    public NetworkSettingsViewModel(IWidgetManagerService widgetManagerService)
    {
        _widgetManagerService = widgetManagerService;
    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Network;

    protected override void InitializeWidgetSettings()
    {

    }
}