// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.ViewModels.Dialogs;

public interface IDialog<TViewModel>
	where TViewModel : class, INotifyPropertyChanged
{
	TViewModel ViewModel { get; set; }

	Task<DialogResult> ShowAsync();

	void Hide();
}
