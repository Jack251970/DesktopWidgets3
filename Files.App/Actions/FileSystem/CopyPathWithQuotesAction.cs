﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions;

internal class CopyPathWithQuotesAction : IAction
{
	private readonly IContentPageContext context;

	public string Label
		=> "CopyPathWithQuotes".ToLocalized();

	public string Description
		=> "CopyPathWithQuotesDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconCopyPath");

	public HotKey HotKey
		=> new(Keys.C, KeyModifiers.MenuCtrl);

	public bool IsExecutable
		=> context.HasSelection;

	public CopyPathWithQuotesAction(IFolderViewViewModel folderViewViewModel)
    {
        context = folderViewViewModel.GetService<IContentPageContext>();
    }

	public Task ExecuteAsync()
	{
		if (context.ShellPage?.SlimContentPage is not null)
		{
			var selectedItems = context.ShellPage.SlimContentPage.SelectedItems;
			var path = selectedItems is not null
				? string.Join("\n", selectedItems.Select(item => $"\"{item.ItemPath}\""))
				: context.ShellPage.FilesystemViewModel.WorkingDirectory;

			if (FtpHelpers.IsFtpPath(path))
            {
                path = path.Replace("\\", "/", StringComparison.Ordinal);
            }

            SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage data = new();
				data.SetText(path);

				Clipboard.SetContent(data);
				Clipboard.Flush();
			});
		}

		return Task.CompletedTask;
	}
}
