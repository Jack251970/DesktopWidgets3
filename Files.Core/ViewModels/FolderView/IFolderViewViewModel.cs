using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.Core.ViewModels.FolderView;

public partial interface IFolderViewViewModel
{
    Window MainWindow { get; }

    Page Page { get; }

    IntPtr WindowHandle { get; }

    event PropertyChangedEventHandler? PropertyChanged;

    int TabStripSelectedIndex { get; set; }

    bool CanShowDialog { get; set; }

    Frame RootFrame { get; }

    TaskCompletionSource? SplashScreenLoadingTCS { get; }

    CommandBarFlyout? LastOpenedFlyout { get; set; }

    T GetService<T>() where T : class;
}
