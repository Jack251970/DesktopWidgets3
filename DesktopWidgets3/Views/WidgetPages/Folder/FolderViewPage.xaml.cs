using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.WidgetsPages.Folder;

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
}
