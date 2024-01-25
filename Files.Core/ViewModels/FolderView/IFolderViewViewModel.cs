using Microsoft.UI.Xaml;

namespace Files.Core.ViewModels.FolderView;

public partial interface IFolderViewViewModel
{
    FileNameConflictResolveOptionType ConflictsResolveOption { get; }

    bool ShowFileExtensions { get; }

    bool ShowDotFiles { get; }

    string WorkingDirectory { get; }

    IDialogService DialogService { get; }

    Window MainWindow { get; }

    IntPtr WindowHandle { get; }

    bool CanShowDialog { get; set; }

    T GetRequiredService<T>() where T : class;
}
