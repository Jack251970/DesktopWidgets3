// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;
using IO = System.IO;

namespace Files.App.Utils;

public sealed class StorageFolderWithPath(BaseStorageFolder folder, string path) : IStorageItemWithPath
{
    public string Path { get; } = path;
    public string Name => Item?.Name ?? IO.Path.GetFileName(Path);

	IStorageItem IStorageItemWithPath.Item => Item;
    public BaseStorageFolder Item { get; } = folder;

    public FilesystemItemType ItemType => FilesystemItemType.Directory;

	public StorageFolderWithPath(BaseStorageFolder folder)
		: this(folder, folder.Path) { }
}
