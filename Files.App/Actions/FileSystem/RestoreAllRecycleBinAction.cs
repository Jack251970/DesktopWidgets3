// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class RestoreAllRecycleBinAction(IFolderViewViewModel folderViewViewModel) : BaseUIAction(folderViewViewModel), IAction
{
    public string Label
		=> "RestoreAllItems".GetLocalizedResource();

	public string Description
		=> "RestoreAllRecycleBinDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconRestoreItem");

	public override bool IsExecutable =>
		FolderViewViewModel.CanShowDialog &&
		RecycleBinHelpers.RecycleBinHasItems();

    public async Task ExecuteAsync(object? parameter = null)
	{
		await RecycleBinHelpers.RestoreRecycleBinAsync(FolderViewViewModel);
	}
}
