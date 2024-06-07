using System.ComponentModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

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

    AppWindow AppWindow { get; }

    Rect Bounds { get; }

    Page Page { get; }

    UIElement Content { get; set; }

    XamlRoot XamlRoot { get; }
    
    TaskCompletionSource? SplashScreenLoadingTCS { get; }

    CommandBarFlyout? LastOpenedFlyout { set; }

    event PropertyChangedEventHandler? PropertyChanged;

    int TabStripSelectedIndex { get; set; }

    bool CanShowDialog { get; set; }

    T GetService<T>() where T : class;
}
