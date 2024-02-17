using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class EditModeOverlayViewModel : ObservableRecipient
{
    public ClickCommand SaveCommand
    {
        get; set;
    }

    public ClickCommand SettingCommand
    {
        get; set;
    }

    public ClickCommand CancelCommand
    {
        get; set;
    }

    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    public EditModeOverlayViewModel(INavigationService navigationService, IWidgetManagerService widgetManagerService)
    {
        _navigationService = navigationService;
        _widgetManagerService = widgetManagerService;

        SaveCommand = new ClickCommand(SaveAndExitEditMode);
        SettingCommand = new ClickCommand(NavigateSettingsPage);
        CancelCommand = new ClickCommand(CancelAndExitEditMode);
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
