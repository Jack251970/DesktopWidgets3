// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class CreateFolderAction : BaseUIAction, IAction
{
    private readonly IContentPageContext context;

	public string Label
		=> "Folder".GetLocalizedResource();

	public string Description
		=> "CreateFolderDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.N, KeyModifiers.CtrlShift);

	public RichGlyph Glyph
		=> new(baseGlyph: "\uE8B7");

	public override bool IsExecutable =>
		context.CanCreateItem &&
		FolderViewViewModel.CanShowDialog;

	public CreateFolderAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(folderViewViewModel)
    {
        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
		if (context.ShellPage is not null)
        {
            _ = UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(FolderViewViewModel, AddItemDialogItemType.Folder, null!, context.ShellPage);
        }

        return Task.CompletedTask;
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
