// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class EmptyRecycleBinAction : BaseUIAction, IAction
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

	public EmptyRecycleBinAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(folderViewViewModel)
    {
        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
	}

	public async Task ExecuteAsync(object? parameter = null)
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
