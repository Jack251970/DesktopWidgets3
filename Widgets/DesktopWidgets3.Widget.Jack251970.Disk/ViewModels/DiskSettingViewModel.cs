namespace DesktopWidgets3.Widget.Jack251970.Disk.ViewModels;

public partial class DiskSettingViewModel : BaseWidgetSettingViewModel
{
    private DiskSettings Settings = null!;

    private bool _initialized = false;

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update properties by settings
        if (settings is DiskSettings diskSettings)
        {
            Settings = diskSettings;

            if (!_initialized)
            {
                _initialized = true;
            }
        }
    }

    #endregion
}
