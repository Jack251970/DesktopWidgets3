using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Navigation;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Views.Pages;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class NavShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    public INavigationService NavigationService
    {
        get;
    }

    public IShellService ShellService
    {
        get;
    }

    public NavShellViewModel(INavigationService navigationService, IShellService shellService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        ShellService = shellService;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (e.SourcePageType == typeof(SettingsPage))
        {
            Selected = ShellService.SettingsItem;
            return;
        }

        var selectedItem = ShellService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }
}
