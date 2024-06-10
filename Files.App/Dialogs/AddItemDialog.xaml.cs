// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Files.App.ViewModels.Dialogs.AddItemDialog;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs;

public sealed partial class AddItemDialog : ContentDialog, IDialog<AddItemDialogViewModel>
{
    private readonly IFolderViewViewModel FolderViewViewModel;

	private readonly IAddItemService addItemService = DependencyExtensions.GetRequiredService<IAddItemService>();

    private FrameworkElement RootAppElement
        => (FrameworkElement)FolderViewViewModel.Content;

    public AddItemDialogViewModel ViewModel
	{
		get => (AddItemDialogViewModel)DataContext;
		set => DataContext = value;
    }

	public AddItemDialog(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

        InitializeComponent();
	}

	public async new Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}

	private void ListView_ItemClick(object sender, ItemClickEventArgs e)
	{
		ViewModel.ResultType = ((AddItemDialogListItemViewModel)e.ClickedItem).ItemResult!;

		Hide();
	}

	private async void AddItemDialog_Loaded(object sender, RoutedEventArgs e)
	{
		var itemTypes = addItemService.GetEntries();
		await ViewModel.AddItemsToListAsync(itemTypes);

		// Focus on the list view so users can use keyboard navigation
		AddItemsListView.Focus(FocusState.Programmatic);
	}
}
