// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class CreateShortcutAction : BaseUIAction, IAction
{
	private readonly IContentPageContext context;

	public string Label
		=> "CreateShortcut".GetLocalizedResource();

	public string Description
		=> "CreateShortcutDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconShortcut");

	public override bool IsExecutable =>
		context.HasSelection &&
		context.CanCreateItem &&
		FolderViewViewModel.CanShowDialog;

	public CreateShortcutAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(folderViewViewModel)
    {
        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
		return UIFilesystemHelpers.CreateShortcutAsync(FolderViewViewModel, context.ShellPage, context.SelectedItems);
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.HasSelection):
			case nameof(IContentPageContext.CanCreateItem):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
