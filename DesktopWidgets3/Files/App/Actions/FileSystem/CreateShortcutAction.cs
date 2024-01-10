// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using Files.App.Data.Commands;
using Files.App.Helpers;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;

namespace Files.App.Actions;

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
        context.PropertyChanged += Context_PropertyChanged;
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
