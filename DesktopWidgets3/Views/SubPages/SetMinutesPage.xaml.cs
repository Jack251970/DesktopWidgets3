using Microsoft.UI.Xaml.Controls;
using DesktopWidgets3.ViewModels.SubPages;

namespace DesktopWidgets3.Views.SubPages;

public sealed partial class SetMinutesPage : Page
{
    public SetMinutesViewModel ViewModel
    {
        get;
    }

    public SetMinutesPage()
    {
        ViewModel = App.GetService<SetMinutesViewModel>();
        InitializeComponent();
    }
}
