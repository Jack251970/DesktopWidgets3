using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class EditModeOverlayViewModel(INavigationService navigationService, IWidgetManagerService widgetManagerService) : ObservableRecipient
{
    private readonly INavigationService _navigationService = navigationService;
    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;

    [RelayCommand]
    private async Task SaveAndExitEditMode()
    {
        await _widgetManagerService.SaveAndExitEditMode();
    }

    [RelayCommand]
    private void NavigateSettingsPage()
    {
        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        App.ShowMainWindow(true);
    }

    [RelayCommand]
    private void CancelChangesAndExitEditMode()
    {
        _widgetManagerService.CancelChangesAndExitEditMode();
    }
}
