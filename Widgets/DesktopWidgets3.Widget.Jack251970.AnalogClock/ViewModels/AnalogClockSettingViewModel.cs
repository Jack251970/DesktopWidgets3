﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock.ViewModels;

public partial class AnalogClockSettingViewModel(WidgetInitContext context) : BaseWidgetSettingViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _showSeconds = true;

    #endregion

    public readonly WidgetInitContext Context = context;

    private AnalogClockSettings Settings = null!;

    private bool _initialized = false;

    partial void OnShowSecondsChanged(bool value)
    {
        if (_initialized)
        {
            Settings.ShowSeconds = value;
            Context.API.UpdateWidgetSettings(this, Settings, true, false);
        }
    }

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update properties by settings
        if (settings is AnalogClockSettings analogClockSettings)
        {
            Settings = analogClockSettings;

            ShowSeconds = Settings.ShowSeconds;

            if (!_initialized)
            {
                _initialized = true;
            }
        }
    }

    #endregion
}
