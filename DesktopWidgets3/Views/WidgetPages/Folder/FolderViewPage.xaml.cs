using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.WidgetsPages.Folder;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.WidgetPages.Folder;

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

    [Obsolete]
    private void FolderViewItemClick(object sender, RoutedEventArgs e)
    {
        ViewModel.FolderViewItemClick(sender);
    }
}
