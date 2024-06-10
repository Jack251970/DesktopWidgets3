// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.ViewModels.Previews;

public sealed class FolderPreviewViewModel
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    /*private static readonly IDateTimeFormatter dateTimeFormatter = DependencyExtensions.GetRequiredService<IDateTimeFormatter>();*/

    public ListedItem Item { get; }

	public BitmapImage Thumbnail { get; set; } = new();

	private BaseStorageFolder Folder { get; set; } = null!;

    public FolderPreviewViewModel(ListedItem item)
    {
        Item = item;

        FolderViewViewModel = item.FolderViewViewModel;
    }

    public Task LoadAsync()
		=> LoadPreviewAndDetailsAsync();

	private async Task LoadPreviewAndDetailsAsync()
	{
		var rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(Item.ItemPath));
		Folder = await StorageFileExtensions.DangerousGetFolderFromPathAsync(Item.ItemPath, rootItem);
		var items = await Folder.GetItemsAsync();

        var result = await FileThumbnailHelper.GetIconAsync(
                Item.ItemPath,
                Constants.ShellIconSizes.Jumbo,
                true,
                IconOptions.None);

        if (result is not null)
        {
            Thumbnail = (await result.ToBitmapAsync())!;
        }

        var info = await Folder.GetBasicPropertiesAsync();

		Item.FileDetails =
        [
            GetFileProperty("PropertyItemCount", items.Count),
			GetFileProperty("PropertyDateModified", info.DateModified),
			GetFileProperty("PropertyDateCreated", info.DateCreated),
			GetFileProperty("PropertyParsingPath", Folder.Path),
		];

		if (GitHelpers.IsRepositoryEx(Item.ItemPath, out var repoPath) &&
			!string.IsNullOrEmpty(repoPath))
		{
			var gitDirectory = GitHelpers.GetGitRepositoryPath(Folder.Path, Path.GetPathRoot(Folder.Path)!);
			var headName = (await GitHelpers.GetRepositoryHead(gitDirectory))?.Name ?? string.Empty;
			var repositoryName = GitHelpers.GetOriginRepositoryName(gitDirectory);

			if(!string.IsNullOrEmpty(gitDirectory))
            {
                Item.FileDetails.Add(GetFileProperty("GitOriginRepositoryName", repositoryName));
            }

            if (!string.IsNullOrWhiteSpace(headName))
            {
                Item.FileDetails.Add(GetFileProperty("GitCurrentBranch", headName));
            }
        }
	}

	private FileProperty GetFileProperty(string nameResource, object value)
		=> new(FolderViewViewModel) { NameResource = nameResource, Value = value };
}
