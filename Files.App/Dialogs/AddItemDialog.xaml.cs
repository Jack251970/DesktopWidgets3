// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs;

public sealed partial class AddItemDialog : ContentDialog, IDialog<AddItemDialogViewModel>
{
	private readonly IAddItemService addItemService = Ioc.Default.GetRequiredService<IAddItemService>();

	public AddItemDialogViewModel ViewModel
	{
		get => (AddItemDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public AddItemDialog()
	{
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
