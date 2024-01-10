// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.App.ViewModels.Dialogs;

class RenameDialogViewModel : ObservableObject
{
	private bool isNameInvalid;
	public bool IsNameInvalid
	{
		get => isNameInvalid;
		set => SetProperty(ref isNameInvalid, value);
	}
}
