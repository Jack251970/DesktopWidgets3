using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public partial class PerformanceSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _useCelsius = false;

    #endregion

    private PerformanceWidgetSettings Settings => (PerformanceWidgetSettings)WidgetSettings!;

    public PerformanceSettingsViewModel()
    {

    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Performance;

    protected override void InitializeWidgetSettings()
    {
        UseCelsius = Settings.UseCelsius;
    }

    partial void OnUseCelsiusChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.UseCelsius = value;
            NeedUpdate = true;
        }
    }
}
