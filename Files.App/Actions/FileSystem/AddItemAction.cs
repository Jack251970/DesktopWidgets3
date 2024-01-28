// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class AddItemAction : ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IContentPageContext context;

	private readonly IDialogService dialogService;

	private readonly AddItemDialogViewModel viewModel = new();

	public string Label
		=> "BaseLayoutContextFlyoutNew/Label".ToLocalized();

	public string Description
		=> "AddItemDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconNew");

	public HotKey HotKey
		=> new(Keys.I, KeyModifiers.CtrlShift);

	public bool IsExecutable
		=> context.CanCreateItem;

	public AddItemAction(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;

        context = folderViewViewModel.GetService<IContentPageContext>();
        dialogService = folderViewViewModel.GetService<IDialogService>();

		context.PropertyChanged += Context_PropertyChanged;
	}

	public async Task ExecuteAsync()
	{
		await dialogService.ShowDialogAsync(viewModel);

		if (viewModel.ResultType.ItemType == AddItemDialogItemType.Shortcut)
		{
			await FolderViewViewModel.GetService<ICommandManager>().CreateShortcutFromDialog.ExecuteAsync();
		}
		else if (viewModel.ResultType.ItemType != AddItemDialogItemType.Cancel)
		{
			await UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(
                FolderViewViewModel,
				viewModel.ResultType.ItemType,
				viewModel.ResultType.ItemInfo,
				context.ShellPage!);
		}

		viewModel.ResultType.ItemType = AddItemDialogItemType.Cancel;
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.CanCreateItem):
			case nameof(IContentPageContext.HasSelection):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
