using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.ViewModels.WidgetsPages.Folder;

public partial class FolderViewViewModel : ObservableRecipient
{
    private readonly DispatcherQueue _dispatcherQueue = App.MainWindow!.DispatcherQueue;

    [ObservableProperty]
    private string _FolderPath = $"D:\\";

    public FolderViewViewModel()
    {

    }
}
