using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace Files.Core.ViewModels.FolderView;

public partial interface IFolderViewViewModel
{
    WindowEx MainWindow { get; }

    IntPtr WindowHandle { get; }

    Page Page { get; }
    
    Frame RootFrame { get; }
    
    TaskCompletionSource? SplashScreenLoadingTCS { get; }

    CommandBarFlyout? LastOpenedFlyout { get; set; }

    event PropertyChangedEventHandler? PropertyChanged;

    int TabStripSelectedIndex { get; set; }

    bool CanShowDialog { get; set; }

    T GetService<T>() where T : class;
}
