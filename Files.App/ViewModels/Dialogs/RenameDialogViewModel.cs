// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.Dialogs;

// TODO: change to internal.
public class RenameDialogViewModel : ObservableObject
{
	private bool isNameInvalid;
	public bool IsNameInvalid
	{
		get => isNameInvalid;
		set => SetProperty(ref isNameInvalid, value);
	}
}

