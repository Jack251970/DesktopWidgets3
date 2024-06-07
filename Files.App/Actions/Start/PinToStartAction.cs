﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;

namespace Files.App.Actions;

internal sealed class PinToStartAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel = folderViewViewModel;

    private IStorageService StorageService { get; } = DependencyExtensions.GetService<IStorageService>();

	private IStartMenuService StartMenuService { get; } = DependencyExtensions.GetService<IStartMenuService>();

	public IContentPageContext context = context;

	public string Label
		=> "PinItemToStart/Text".GetLocalizedResource();

	public string Description
		=> "PinToStartDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "Icons.Pin.16x16");

	public bool IsExecutable =>
		context.ShellPage is not null;

    public async Task ExecuteAsync(object? parameter = null)
	{
		if (context.SelectedItems.Count > 0 && context.ShellPage?.SlimContentPage?.SelectedItems is not null)
		{
            foreach (var listedItem in context.ShellPage.SlimContentPage.SelectedItems)
            {
                IStorable storable = listedItem.IsFolder switch
                {
                    true => await StorageService.GetFolderAsync(listedItem.ItemPath),
                    _ => await StorageService.GetFileAsync((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath)
                };
                await StartMenuService.PinAsync(FolderViewViewModel, storable, listedItem.Name);
            }
        }
		else if (context.ShellPage?.FilesystemViewModel?.CurrentFolder is not null)
		{
			var currentFolder = context.ShellPage.FilesystemViewModel.CurrentFolder;
			var folder = await StorageService.GetFolderAsync(currentFolder.ItemPath);

			await StartMenuService.PinAsync(FolderViewViewModel, folder, currentFolder.Name);
		}
	}
}
