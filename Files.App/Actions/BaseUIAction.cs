// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

/// <summary>
/// Represents base class for the UI Actions.
/// </summary>
internal abstract class BaseUIAction : ObservableObject
{
    protected readonly IFolderViewViewModel FolderViewViewModel;

    public virtual bool IsExecutable
		=> FolderViewViewModel.CanShowDialog;

	public BaseUIAction(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

        FolderViewViewModel.PropertyChanged += UIHelpers_PropertyChanged;
	}

	private void UIHelpers_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(FolderViewViewModel.CanShowDialog))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
