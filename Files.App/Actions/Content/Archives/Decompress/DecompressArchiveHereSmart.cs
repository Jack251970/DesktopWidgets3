﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class DecompressArchiveHereSmart : BaseDecompressArchiveAction
{
	public override string Label
		=> "ExtractHereSmart".GetLocalizedResource();

	public override string Description
		=> "DecompressArchiveHereSmartDescription".GetLocalizedResource();

	public override HotKey HotKey
		=> new(Keys.E, KeyModifiers.CtrlShift);

	public DecompressArchiveHereSmart(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(folderViewViewModel, context)
    {
    }

    public override Task ExecuteAsync()
	{
		return DecompressHelper.DecompressArchiveHereAsync(FolderViewViewModel, context.ShellPage!, true);
	}
}
