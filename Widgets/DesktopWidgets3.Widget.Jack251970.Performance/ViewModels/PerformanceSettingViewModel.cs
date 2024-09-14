using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.Performance.ViewModels;

public partial class PerformanceSettingViewModel(WidgetInitContext context) : BaseWidgetSettingViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _useCelsius = false;

    #endregion

    public readonly WidgetInitContext Context = context;

    private PerformanceSettings Settings = null!;

    private bool _initialized = false;

    partial void OnUseCelsiusChanged(bool value)
    {
        if (_initialized)
        {
            Settings.UseCelsius = value;
            Context.API.UpdateWidgetSettings(this, Settings, true, false);
        }
    }

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update properties by settings
        if (settings is PerformanceSettings performanceSettings)
        {
            Settings = performanceSettings;

            UseCelsius = Settings.UseCelsius;

            if (!_initialized)
            {
                _initialized = true;
            }
        }
    }

    #endregion
}
