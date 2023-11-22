using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;

namespace DesktopWidgets3.ViewModels.WidgetsPages.Clock;

public partial class ClockViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _appDisplayName;

    public ClockViewModel()
    {
        _appDisplayName = "AppDisplayName".GetLocalized();
    }
}
