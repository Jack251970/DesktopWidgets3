using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Widget.DigitalClock.Setting;

namespace DesktopWidgets3.Widget.DigitalClock.ViewModels;

public partial class DigitalClockSettingViewModel(WidgetInitContext context) : BaseWidgetSettingViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _showSeconds = true;

    #endregion

    public readonly WidgetInitContext Context = context;

    private DigitalClockSettings Settings = null!;

    #region abstract methods

    public override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        if (settings is DigitalClockSettings digitalClockSetting)
        {
            Settings = digitalClockSetting;
        }

        if (initialized)
        {
            ShowSeconds = Settings.ShowSeconds;
        }
    }

    #endregion

    partial void OnShowSecondsChanged(bool value)
    {
        if (Settings != null)
        {
            Settings.ShowSeconds = value;
            Context.API.UpdateWidgetSettings(this, Settings);
        }
    }
}
