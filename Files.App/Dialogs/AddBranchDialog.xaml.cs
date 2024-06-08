// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs;

public sealed partial class AddBranchDialog : ContentDialog, IDialog<AddBranchDialogViewModel>
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private FrameworkElement RootAppElement
        => (FrameworkElement)FolderViewViewModel.Content;

    public AddBranchDialogViewModel ViewModel
	{
		get => (AddBranchDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public AddBranchDialog(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

		InitializeComponent();
	}

	public async new Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

	private void ContentDialog_Closing(ContentDialog _, ContentDialogClosingEventArgs e)
	{
		InvalidNameWarning.IsOpen = false;
		Closing -= ContentDialog_Closing;
	}
}
