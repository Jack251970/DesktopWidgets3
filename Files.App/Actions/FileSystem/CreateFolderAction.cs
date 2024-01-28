// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class CreateFolderAction : BaseUIAction, IAction
{
    private readonly IContentPageContext context;

	public string Label
		=> "Folder".ToLocalized();

	public string Description
		=> "CreateFolderDescription".ToLocalized();

	public HotKey HotKey
		=> new(Keys.N, KeyModifiers.CtrlShift);

	public RichGlyph Glyph
		=> new(baseGlyph: "\uE8B7");

	public override bool IsExecutable =>
		context.CanCreateItem &&
		FolderViewViewModel.CanShowDialog;

	public CreateFolderAction(IFolderViewViewModel folderViewViewModel) : base(folderViewViewModel)
	{
        context = folderViewViewModel.GetService<IContentPageContext>();

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
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
