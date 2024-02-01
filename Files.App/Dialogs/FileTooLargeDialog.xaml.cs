// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs;

public sealed partial class FileTooLargeDialog : ContentDialog, IDialog<FileTooLargeDialogViewModel>
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    public FileTooLargeDialogViewModel ViewModel
	{
		get => (FileTooLargeDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public FileTooLargeDialog(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

        InitializeComponent();
	}

	public async new Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await SetContentDialogRoot(this).TryShowAsync(FolderViewViewModel);
	}

	// WINUI3
	private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
	{
		if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            contentDialog.XamlRoot = FolderViewViewModel.MainWindow.Content.XamlRoot;
        }

        return contentDialog;
	}
}
