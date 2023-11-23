using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class HomeViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _appDisplayName;

    public HomeViewModel(IWidgetManagerService widgetManagerService)
    {
        _appDisplayName = "AppDisplayName".GetLocalized();
        widgetManagerService.InitializeWidgets();
    }
}
