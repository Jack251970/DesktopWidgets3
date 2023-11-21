using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using DesktopWidgets3.Contracts.Services;

namespace DesktopWidgets3.Views.Controls;

public sealed partial class TrayMenuControl : UserControl
{
    private readonly ITimersService _timersService = App.GetService<ITimersService>();

    public TrayMenuControl()
    {
        InitializeComponent();
    }

    private void ExitApp(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        App.EnableCloseWindow();
        App.MainWindow!.Close();
    }

    private void ShowWindow(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        App.ShowMainWindow(false);
        _timersService.StartUpdateTimeTimer();
    }
}