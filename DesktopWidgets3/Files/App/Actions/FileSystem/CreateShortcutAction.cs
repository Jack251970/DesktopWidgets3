// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using DesktopWidgets3.Files.App.Data.Commands;
using DesktopWidgets3.Files.App.Helpers;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;

namespace DesktopWidgets3.Files.App.Actions;

internal class CreateShortcutAction : BaseUIAction, IAction
{
    public string Label
		=> "CreateShortcut".GetLocalized();

	public string Description
		=> "CreateShortcutDescription".GetLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconShortcut");

	public override bool IsExecutable =>
		context.HasSelection &&
		context.CanCreateItem &&
		context.CanShowDialog;

	public CreateShortcutAction(FolderViewViewModel viewModel) : base(viewModel)
    {
    }

	public Task ExecuteAsync()
	{
		return UIFileSystemHelpers.CreateShortcutAsync(context, context.SelectedItems);
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(context.HasSelection):
			case nameof(context.CanCreateItem):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
