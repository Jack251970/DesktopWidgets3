using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.Network.ViewModels;

public partial class NetworkSettingViewModel(WidgetInitContext context) : BaseWidgetSettingViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _useBps = false;

    #endregion

    public readonly WidgetInitContext Context = context;

    private NetworkSettings Settings = null!;

    private bool _initialized = false;

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update properties by settings
        if (settings is NetworkSettings networkSettings)
        {
            Settings = networkSettings;

            UseBps = Settings.UseBps;

            if (!_initialized)
            {
                _initialized = true;
            }
        }
    }

    #endregion

    partial void OnUseBpsChanged(bool value)
    {
        if (_initialized)
        {
            Settings.UseBps = value;
            Context.WidgetService.UpdateWidgetSettings(this, Settings, true, false);
        }
    }
}
