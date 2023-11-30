using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget.FolderView;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Models.Widget;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using DesktopWidgets3.Contracts.Services;

namespace DesktopWidgets3.Views.Pages.Widget.FolderView;

public sealed partial class FolderViewPage : Page
{
    public FolderViewViewModel ViewModel
    {
        get;
    }

    public FolderViewPage()
    {
        ViewModel = App.GetService<FolderViewViewModel>();
        InitializeComponent();
    }

    private async void NavigateBackButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NavigateBackButtonClick();
    }

    private async void NavigateUpButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NavigateUpButtonClick();
    }

    private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: FolderViewFileItem item })
        {
            var filePath = item.FilePath;
            await ViewModel.FolderViewItemDoubleTapped(filePath);
        }
    }

    private void MenuFlyoutItemEnterEidtMode_Click(object sender, RoutedEventArgs e)
    {
        App.GetService<IWidgetManagerService>().EnterEditMode();
    }

    private void MenuFlyoutItemExitEditModeAndSave_Click(object sender, RoutedEventArgs e)
    {
        App.GetService<IWidgetManagerService>().ExitEditModeAndSave();
    }

    private void Toolbar_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            element.ContextFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }
}
