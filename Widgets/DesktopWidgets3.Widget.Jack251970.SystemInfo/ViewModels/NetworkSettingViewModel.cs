﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.ViewModels;

public partial class NetworkSettingViewModel(string widgetId) : ObservableRecipient
{
    #region view properties

    [ObservableProperty]
    private bool _useBps = false;

    #endregion

    public string Id = widgetId;

    private NetworkSettings Settings = null!;

    private bool _initialized = false;

    partial void OnUseBpsChanged(bool value)
    {
        if (_initialized)
        {
            Settings.UseBps = value;
            Main.WidgetInitContext.WidgetService.UpdateWidgetSettingsAsync(Id, Settings);
        }
    }

    #region Settings Methods

    public void LoadSettings(BaseWidgetSettings settings)
    {
        if (settings is NetworkSettings networkSettings)
        {
            // initialize settings instance
            if (!_initialized)
            {
                Settings = networkSettings;
                _initialized = true;
            }

            // update settings
            UseBps = Settings.UseBps;
        }
    }

    #endregion
}
