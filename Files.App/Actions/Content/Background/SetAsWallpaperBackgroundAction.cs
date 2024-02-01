// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class SetAsWallpaperBackgroundAction : BaseSetAsAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    public override string Label
		=> "SetAsBackground".GetLocalizedResource();

	public override string Description
		=> "SetAsWallpaperBackgroundDescription".GetLocalizedResource();

	public override RichGlyph Glyph
		=> new("\uE91B");

	public override bool IsExecutable =>
		base.IsExecutable &&
		context.SelectedItem is not null;

    public SetAsWallpaperBackgroundAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(context)
    {
        FolderViewViewModel = folderViewViewModel;
    }

    public override Task ExecuteAsync()
	{
		if (context.SelectedItem is not null)
        {
            return WallpaperHelpers.SetAsBackgroundAsync(FolderViewViewModel, WallpaperType.Desktop, context.SelectedItem.ItemPath);
        }

        return Task.CompletedTask;
	}
}
