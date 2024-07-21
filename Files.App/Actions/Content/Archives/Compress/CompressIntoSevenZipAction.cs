﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class CompressIntoSevenZipAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : BaseCompressArchiveAction(folderViewViewModel, context)
{
    public override string Label
            => string.Format("CreateNamedArchive".GetLocalizedResource(), $"{StorageArchiveService.GenerateArchiveNameFromItems(context.SelectedItems)}.7z");

	public override string Description
		=> "CompressIntoSevenZipDescription".GetLocalizedResource();

    public override Task ExecuteAsync(object? parameter = null)
	{
		if (context.ShellPage is null)
        {
            return Task.CompletedTask;
        }

        GetDestination(out var sources, out var directory, out var fileName);

        ICompressArchiveModel compressionModel = new CompressArchiveModel(
            sources,
            directory,
            fileName,
            fileFormat: ArchiveFormats.SevenZip);

        return StorageArchiveService.CompressAsync(FolderViewViewModel, compressionModel);
    }
}
