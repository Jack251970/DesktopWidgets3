using Microsoft.UI.Xaml;

namespace Files.Core.ViewModels.Widgets.FolderView;

public partial interface IFolderViewViewModel
{
    int IndexTag { get; }

    FileNameConflictResolveOptionType ConflictsResolveOption { get; }

    string WorkingDirectory { get; }

    IDialogService DialogService { get; }

    Window MainWindow { get; }

    bool CanShowDialog { get; set; }
}
