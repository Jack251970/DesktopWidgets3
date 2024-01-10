// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Data.Commands;
using Files.App.Helpers;
using Files.Core.Data.Enums;
using Files.Core.Services;
using Files.Core.ViewModels.Dialogs.AddItemDialog;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;

namespace Files.App.Actions;

internal class AddItemAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    private readonly IDialogService dialogService;

	private readonly AddItemDialogViewModel viewModel = new();

	public string Label
		=> "BaseLayoutContextFlyoutNew/Label".GetLocalized();

	public string Description
		=> "AddItemDescription".GetLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconNew");

	/*public HotKey HotKey
		=> new(Keys.I, KeyModifiers.CtrlShift);*/

	public bool IsExecutable
		=> context.CanCreateItem;

	public AddItemAction(FolderViewViewModel viewModel)
    {
        context = viewModel;
        dialogService = viewModel.DialogService;

        context.PropertyChanged += Context_PropertyChanged;
	}

	public async Task ExecuteAsync()
	{
		await dialogService.ShowDialogAsync(viewModel);

		if (viewModel.ResultType.ItemType == AddItemDialogItemType.Shortcut)
		{
			await context.CommandManager.CreateShortcutFromDialog.ExecuteAsync();
		}
		else if (viewModel.ResultType.ItemType != AddItemDialogItemType.Cancel)
		{
			await UIFileSystemHelpers.CreateFileFromDialogResultTypeAsync(
				viewModel.ResultType.ItemType,
				viewModel.ResultType.ItemInfo,
				context!);
		}

		viewModel.ResultType.ItemType = AddItemDialogItemType.Cancel;
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(context.CanCreateItem):
			case nameof(context.HasSelection):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
