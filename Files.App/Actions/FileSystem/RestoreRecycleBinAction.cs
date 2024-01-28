// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class RestoreRecycleBinAction : BaseUIAction, IAction
{
	private readonly IContentPageContext context;

	public string Label
		=> "Restore".ToLocalized();

	public string Description
		=> "RestoreRecycleBinDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconRestoreItem");

	public override bool IsExecutable =>
		context.PageType is ContentPageTypes.RecycleBin &&
		context.SelectedItems.Any() &&
		FolderViewViewModel.CanShowDialog;

	public RestoreRecycleBinAction(IFolderViewViewModel folderViewViewModel) : base(folderViewViewModel)
    {
        context = folderViewViewModel.GetService<IContentPageContext>();

        context.PropertyChanged += Context_PropertyChanged;
	}

	public async Task ExecuteAsync()
	{
		if (context.ShellPage is not null)
        {
            await RecycleBinHelpers.RestoreSelectionRecycleBinAsync(FolderViewViewModel, context.ShellPage);
        }
    }

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.PageType):
			case nameof(IContentPageContext.SelectedItems):
				if (context.PageType is ContentPageTypes.RecycleBin)
                {
                    OnPropertyChanged(nameof(IsExecutable));
                }

                break;
		}
	}
}
