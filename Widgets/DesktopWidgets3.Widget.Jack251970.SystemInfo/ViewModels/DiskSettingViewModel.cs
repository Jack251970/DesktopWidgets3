using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.ViewModels;

public partial class DiskSettingViewModel(string widgetId) : ObservableRecipient
{
    public string Id = widgetId;

    private DiskSettings Settings = null!;

    private bool _initialized = false;

    #region Settings Methods

    public void LoadSettings(BaseWidgetSettings baseSettings)
    {
        if (baseSettings is DiskSettings settings)
        {
            // update settings

            // initialize settings instance
            if (!_initialized)
            {
                Settings = settings;
                _initialized = true;
            }
        }
    }

    #endregion
}
