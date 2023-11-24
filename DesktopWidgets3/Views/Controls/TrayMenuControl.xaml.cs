using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Controls;

public sealed partial class TrayMenuControl : UserControl
{
    public TrayMenuControl()
    {
        InitializeComponent();
    }

    private void ExitApp(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        App.CanCloseWindow = true;
        App.MainWindow!.Close();
    }

    private void ShowWindow(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        App.ShowMainWindow(false);
    }
}