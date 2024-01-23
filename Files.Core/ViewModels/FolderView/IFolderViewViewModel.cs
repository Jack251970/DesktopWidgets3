namespace Files.Core.ViewModels.Widgets.FolderView;

public interface IFolderViewViewModel
{
    int IndexTag { get; }

    FileNameConflictResolveOptionType ConflictsResolveOption { get; }

    string WorkingDirectory { get; }

    IDialogService DialogService { get; }
}
