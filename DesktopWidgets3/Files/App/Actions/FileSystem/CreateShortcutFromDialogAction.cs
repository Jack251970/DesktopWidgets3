// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using Files.App.Data.Commands;
using Files.App.Helpers;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;

namespace Files.App.Actions;

internal class CreateShortcutFromDialogAction : BaseUIAction, IAction
{
	public string Label
		=> "Shortcut".GetLocalized();

	public string Description
		=> "CreateShortcutFromDialogDescription".GetLocalized();

	public RichGlyph Glyph
		=> new("\uE71B");

	public override bool IsExecutable =>
		context.CanCreateItem &&
		context.CanShowDialog;

	public CreateShortcutFromDialogAction(FolderViewViewModel viewModel) : base(viewModel)
	{
		context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
	{
		return UIFileSystemHelpers.CreateShortcutFromDialogAsync(context!);
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
        if (e.PropertyName is nameof(context.CanCreateItem))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
