// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs;

public sealed partial class ElevateConfirmDialog : ContentDialog, IDialog<ElevateConfirmDialogViewModel>
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private FrameworkElement RootAppElement
        => (FrameworkElement)FolderViewViewModel.Content;

    public ElevateConfirmDialogViewModel ViewModel
	{
		get => (ElevateConfirmDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public ElevateConfirmDialog(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;

        InitializeComponent();
	}

	public async new Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}
}
