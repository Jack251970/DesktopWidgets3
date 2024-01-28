// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class CreateShortcutFromDialogAction : BaseUIAction, IAction
{
	private readonly IContentPageContext context;

	public string Label
		=> "Shortcut".GetLocalizedResource();

	public string Description
		=> "CreateShortcutFromDialogDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new("\uE71B");

	public override bool IsExecutable =>
		context.CanCreateItem &&
		FolderViewViewModel.CanShowDialog;

	public CreateShortcutFromDialogAction(IFolderViewViewModel folderViewViewModel) : base(folderViewViewModel)
	{
		context = folderViewViewModel.GetService<IContentPageContext>();

		context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
	{
		return UIFilesystemHelpers.CreateShortcutFromDialogAsync(FolderViewViewModel, context.ShellPage!);
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IContentPageContext.CanCreateItem))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
