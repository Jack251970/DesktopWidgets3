﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class CompressIntoSevenZipAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : BaseCompressArchiveAction(folderViewViewModel, context)
{
	public override string Label
		=> string.Format("CreateNamedArchive".GetLocalizedResource(), $"{CompressHelper.DetermineArchiveNameFromSelection(context.SelectedItems)}.7z");

	public override string Description
		=> "CompressIntoSevenZipDescription".GetLocalizedResource();

    public override Task ExecuteAsync(object? parameter = null)
	{
		if (context.ShellPage is null)
        {
            return Task.CompletedTask;
        }

        var (sources, directory, fileName) = CompressHelper.GetCompressDestination(context.ShellPage);

		ICompressArchiveModel creator = new CompressArchiveModel(
			sources,
			directory,
			fileName,
			fileFormat: ArchiveFormats.SevenZip);

		return CompressHelper.CompressArchiveAsync(FolderViewViewModel, creator);
	}
}
