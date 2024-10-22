using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.ViewModels;

public partial class PerformanceSettingViewModel(string widgetId) : ObservableRecipient
{
    #region view properties

    [ObservableProperty]
    private bool _useCelsius = false;

    #endregion

    public string Id = widgetId;

    private PerformanceSettings Settings = null!;

    private bool _initialized = false;

    partial void OnUseCelsiusChanged(bool value)
    {
        if (_initialized)
        {
            Settings.UseCelsius = value;
            Main.WidgetInitContext.WidgetService.UpdateWidgetSettingsAsync(Id, Settings);
        }
    }

    #region Settings Methods

    public void LoadSettings(BaseWidgetSettings settings)
    {
        if (settings is PerformanceSettings performanceSettings)
        {
            // initialize settings instance
            if (!_initialized)
            {
                Settings = performanceSettings;
                _initialized = true;
            }
            
            // update settings
            UseCelsius = Settings.UseCelsius;
        }
    }

    #endregion
}
