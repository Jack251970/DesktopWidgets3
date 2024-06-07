// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Dialogs;

public sealed partial class CreateShortcutDialog : ContentDialog, IDialog<CreateShortcutDialogViewModel>
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private FrameworkElement RootAppElement
        => (FrameworkElement)FolderViewViewModel.Content;

    public CreateShortcutDialogViewModel ViewModel
	{
		get => (CreateShortcutDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public CreateShortcutDialog(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

		InitializeComponent();
		Closing += CreateShortcutDialog_Closing;

		InvalidPathWarning.SetBinding(TeachingTip.TargetProperty, new Binding()
		{
			Source = DestinationItemPath
		});
	}

	private void CreateShortcutDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
	{
		Closing -= CreateShortcutDialog_Closing;
		InvalidPathWarning.IsOpen = false;
	}

	public async new Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}
}
