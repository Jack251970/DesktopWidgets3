// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;
using IO = System.IO;

namespace Files.App.Utils;

public sealed class StorageFileWithPath(BaseStorageFile file, string path) : IStorageItemWithPath
{
    public string Path { get; } = path;
    public string Name => Item?.Name ?? IO.Path.GetFileName(Path);

	IStorageItem IStorageItemWithPath.Item => Item;
    public BaseStorageFile Item { get; } = file;

    public FilesystemItemType ItemType => FilesystemItemType.File;

	public StorageFileWithPath(BaseStorageFile file)
		: this(file, file.Path) { }
}
