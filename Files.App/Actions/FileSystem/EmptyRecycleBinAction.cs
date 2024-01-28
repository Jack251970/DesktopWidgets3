// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class EmptyRecycleBinAction : BaseUIAction, IAction
{
	private readonly IContentPageContext context;

	public string Label
		=> "EmptyRecycleBin".GetLocalizedResource();

	public string Description
		=> "EmptyRecycleBinDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconDelete");

	public override bool IsExecutable =>
		FolderViewViewModel.CanShowDialog &&
		((context.PageType == ContentPageTypes.RecycleBin && context.HasItem) ||
		RecycleBinHelpers.RecycleBinHasItems());

	public EmptyRecycleBinAction(IFolderViewViewModel folderViewViewModel) : base(folderViewViewModel)
    {
		context = FolderViewViewModel.GetService<IContentPageContext>();

		context.PropertyChanged += Context_PropertyChanged;
	}

	public async Task ExecuteAsync()
	{
		await RecycleBinHelpers.EmptyRecycleBinAsync(FolderViewViewModel);
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.PageType):
			case nameof(IContentPageContext.HasItem):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
