using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class HomeViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _appDisplayName;

    public HomeViewModel()
    {
        AppDisplayName = "AppDisplayName".GetLocalized();
#if DEBUG
        AppDisplayName += " (Debug)";
#endif
    }
}
