﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Actions;

internal sealed class CompressIntoArchiveAction : BaseCompressArchiveAction
{
	public override string Label
		=> "CreateArchive".GetLocalizedResource();

	public override string Description
		=> "CompressIntoArchiveDescription".GetLocalizedResource();

	public CompressIntoArchiveAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(folderViewViewModel, context)
    {
	}

	public async override Task ExecuteAsync()
	{
		if (context.ShellPage is null)
        {
            return;
        }

        var (sources, directory, fileName) = CompressHelper.GetCompressDestination(context.ShellPage);

		var dialog = new CreateArchiveDialog(FolderViewViewModel)
		{
			FileName = fileName,
		};

		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            dialog.XamlRoot = FolderViewViewModel.MainWindow.Content.XamlRoot;
        }

        var result = await dialog.TryShowAsync(FolderViewViewModel);

		if (!dialog.CanCreate || result != ContentDialogResult.Primary)
        {
            return;
        }

        ICompressArchiveModel creator = new CompressArchiveModel(
			sources,
			directory,
			dialog.FileName,
			dialog.Password,
			dialog.FileFormat,
			dialog.CompressionLevel,
			dialog.SplittingSize);

		await CompressHelper.CompressArchiveAsync(FolderViewViewModel, creator);
	}
}
