using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;
using DesktopWidgets3.Contracts.Services;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class EditModeOverlayPage : Page
{
    public EditModeOverlayViewModel ViewModel
    {
        get;
    }

    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    public EditModeOverlayPage()
    {
        ViewModel = App.GetService<EditModeOverlayViewModel>();
        InitializeComponent();

        _navigationService = App.GetService<INavigationService>();
        _widgetManagerService = App.GetService<IWidgetManagerService>();
    }

    private void EditModeOverlayPageSaveButton_Click(object sender, RoutedEventArgs e)
    {
        _widgetManagerService.ExitEditModeAndSave();
    }

    private void EditModeOverlayPageSettingButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        App.ShowMainWindow(true);
    }

    private void EditModeOverlayPageCancelButton_Click(object sender, RoutedEventArgs e)
    {
        _widgetManagerService.ExitEditModeAndCancel();
    }
}
