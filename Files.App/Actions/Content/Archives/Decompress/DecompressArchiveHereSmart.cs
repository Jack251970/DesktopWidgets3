// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class DecompressArchiveHereSmart(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : BaseDecompressArchiveAction(folderViewViewModel, context)
{
	public override string Label
		=> "ExtractHereSmart".GetLocalizedResource();

	public override string Description
		=> "DecompressArchiveHereSmartDescription".GetLocalizedResource();

	public override HotKey HotKey
		=> new(Keys.E, KeyModifiers.CtrlShift);

    public override Task ExecuteAsync(object? parameter = null)
    {
        return DecompressArchiveHereAsync(true);
    }
}
