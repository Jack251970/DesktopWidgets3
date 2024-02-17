using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class EditModeOverlayPage : Page
{
    public EditModeOverlayViewModel ViewModel
    {
        get;
    }

    public EditModeOverlayPage()
    {
        ViewModel = App.GetService<EditModeOverlayViewModel>();
        InitializeComponent();
    }
}
