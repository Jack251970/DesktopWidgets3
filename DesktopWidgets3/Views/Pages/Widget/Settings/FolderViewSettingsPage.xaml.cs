using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets.Settings;

public sealed partial class FolderViewSettingsPage : Page
{
    public FolderViewSettingsViewModel ViewModel
    {
        get;
    }

    public FolderViewSettingsPage()
    {
        ViewModel = App.GetService<FolderViewSettingsViewModel>();
        InitializeComponent();
    }
}
