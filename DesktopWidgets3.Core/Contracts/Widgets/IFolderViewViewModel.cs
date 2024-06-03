using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Core.Contracts.Widgets;

public partial interface IFolderViewViewModel
{
    #region widget settings

    bool AllowNavigation { get; }

    event Action<string>? FolderPathChanged;

    #endregion

    #region right tapped menu

    void RegisterRightTappedMenu(FrameworkElement element);

    #endregion

    WindowEx MainWindow { get; }

    IntPtr WindowHandle { get; }

    Page Page { get; }

    UIElement MainWindowContent { get; }

    XamlRoot XamlRoot { get; }
    
    TaskCompletionSource? SplashScreenLoadingTCS { get; }

    CommandBarFlyout? LastOpenedFlyout { set; }

    event PropertyChangedEventHandler? PropertyChanged;

    int TabStripSelectedIndex { get; set; }

    bool CanShowDialog { get; set; }

    T GetService<T>() where T : class;
}
