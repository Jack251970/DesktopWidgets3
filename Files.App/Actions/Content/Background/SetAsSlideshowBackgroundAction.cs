// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class SetAsSlideshowBackgroundAction : BaseSetAsAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    public override string Label
		=> "SetAsSlideshow".GetLocalizedResource();

	public override string Description
		=> "SetAsSlideshowBackgroundDescription".GetLocalizedResource();

	public override RichGlyph Glyph
		=> new("\uE91B");

	public override bool IsExecutable =>
		base.IsExecutable &&
		context.SelectedItems.Count > 1;

    public SetAsSlideshowBackgroundAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(context) => FolderViewViewModel = folderViewViewModel;

    public override Task ExecuteAsync(object? parameter = null)
	{
		var paths = context.SelectedItems.Select(item => item.ItemPath).ToArray();
		WallpaperHelpers.SetSlideshow(FolderViewViewModel, paths);

		return Task.CompletedTask;
	}
}
