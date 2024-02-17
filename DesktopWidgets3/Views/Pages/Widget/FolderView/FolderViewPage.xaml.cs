using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets;

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

        ViewModel.WidgetPage = this;
    }
}
