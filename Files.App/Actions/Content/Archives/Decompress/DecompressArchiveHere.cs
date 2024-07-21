// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class DecompressArchiveHere(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : BaseDecompressArchiveAction(folderViewViewModel, context)
{
	public override string Label
		=> "ExtractHere".GetLocalizedResource();

	public override string Description
		=> "DecompressArchiveHereDescription".GetLocalizedResource();

    public override Task ExecuteAsync(object? parameter = null)
    {
        return DecompressArchiveHereAsync();
    }
}
