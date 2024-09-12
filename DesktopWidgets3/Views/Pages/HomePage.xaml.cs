using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<HomeViewModel>();
        InitializeComponent();
    }
}
