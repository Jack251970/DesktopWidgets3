using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.ViewModels.Commands;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class EditModeOverlayViewModel : ObservableRecipient
{
    public ButtonClickCommand SaveEventHandler
    {
        get; set;
    }

    public ButtonClickCommand SettingEventHandler
    {
        get; set;
    }

    public ButtonClickCommand CancelEventHandler
    {
        get; set;
    }

    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    public EditModeOverlayViewModel(INavigationService navigationService, IWidgetManagerService widgetManagerService)
    {
        _navigationService = navigationService;
        _widgetManagerService = widgetManagerService;

        SaveEventHandler = new ButtonClickCommand(SaveAndExitEditMode);
        SettingEventHandler = new ButtonClickCommand(NavigateSettingsPage);
        CancelEventHandler = new ButtonClickCommand(CancelAndExitEditMode);
    }

    private void SaveAndExitEditMode()
    {
        _widgetManagerService.SaveAndExitEditMode();
    }

    private void NavigateSettingsPage()
    {
        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        App.ShowMainWindow(true);
    }

    private void CancelAndExitEditMode()
    {
        _widgetManagerService.CancelAndExitEditMode();
    }
}
