using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

public partial class ClockSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _showSeconds = true;

    #endregion

    private readonly IWidgetManagerService _widgetManagerService;

    private ClockWidgetSettings Settings => (ClockWidgetSettings)WidgetSettings!;

    public ClockSettingsViewModel(IWidgetManagerService widgetManagerService)
    {
        _widgetManagerService = widgetManagerService;
    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Clock;

    protected override void InitializeWidgetSettings()
    {
        ShowSeconds = Settings.ShowSeconds;
    }

    partial void OnShowSecondsChanged(bool value)
    {
        if (_isInitialized)
        {
            Settings.ShowSeconds = value;
            _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
        }
    }
}
