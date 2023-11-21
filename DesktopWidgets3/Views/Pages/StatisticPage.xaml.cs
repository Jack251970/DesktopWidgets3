using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class StatisticPage : Page
{
    public StatisticViewModel ViewModel
    {
        get;
    }

    public StatisticPage()
    {
        ViewModel = App.GetService<StatisticViewModel>();
        InitializeComponent();
    }
}
