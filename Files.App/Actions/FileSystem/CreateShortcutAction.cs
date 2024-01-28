// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class CreateShortcutAction : BaseUIAction, IAction
{
	private readonly IContentPageContext context;

	public string Label
		=> "CreateShortcut".ToLocalized();

	public string Description
		=> "CreateShortcutDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconShortcut");

	public override bool IsExecutable =>
		context.HasSelection &&
		context.CanCreateItem &&
		FolderViewViewModel.CanShowDialog;

	public CreateShortcutAction(IFolderViewViewModel folderViewViewModel) : base(folderViewViewModel)
	{
        context = folderViewViewModel.GetService<IContentPageContext>();

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
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
