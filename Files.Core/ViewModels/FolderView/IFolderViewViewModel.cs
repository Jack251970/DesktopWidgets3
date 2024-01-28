using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.Core.ViewModels.FolderView;

public partial interface IFolderViewViewModel
{
    Window MainWindow { get; }

    IntPtr WindowHandle { get; }

    bool CanShowDialog { get; set; }

    event PropertyChangedEventHandler? PropertyChanged;

    Frame RootFrame { get; }

    CommandBarFlyout? LastOpenedFlyout { get; set; }

    T GetService<T>() where T : class;
}
