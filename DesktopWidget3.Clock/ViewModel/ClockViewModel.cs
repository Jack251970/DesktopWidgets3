using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidget3.Clock.ViewModel;

public class ClockViewModel
{
    #region view properties

    [ObservableProperty]
    private string _systemTime = string.Empty;

    #endregion
}
