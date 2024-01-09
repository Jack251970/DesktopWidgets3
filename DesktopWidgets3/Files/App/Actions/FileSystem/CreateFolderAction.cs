// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using DesktopWidgets3.Files.App.Data.Commands;
using DesktopWidgets3.Files.App.Helpers;
using DesktopWidgets3.Files.Core.Data.Enums;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;

namespace DesktopWidgets3.Files.App.Actions;

internal class CreateFolderAction : BaseUIAction, IAction
{
	public string Label
		=> "Folder".GetLocalized();

	public string Description
		=> "CreateFolderDescription".GetLocalized();

	/*public HotKey HotKey
		=> new(Keys.N, KeyModifiers.CtrlShift);*/

	public RichGlyph Glyph
		=> new(baseGlyph: "\uE8B7");

	public override bool IsExecutable =>
		context.CanCreateItem &&
		context.CanShowDialog;

	public CreateFolderAction(FolderViewViewModel viewModel) : base(viewModel)
	{
		context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
	{
		if (context is not null)
        {
            return UIFileSystemHelpers.CreateFileFromDialogResultTypeAsync(AddItemDialogItemType.Folder, null!, context);
        }

        return Task.CompletedTask;
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
