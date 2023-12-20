// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;

namespace Files.App.Utils.Storage.Helpers;

public static class StorageFileExtensions
{
    public static BaseStorageFile? AsBaseStorageFile(this IStorageItem item)
    {
        if (item is null || !item.IsOfType(StorageItemTypes.File))
        {
            return null;
        }

        return item is StorageFile file ? (BaseStorageFile)file : item as BaseStorageFile;
    }

    public static BaseStorageFolder? AsBaseStorageFolder(this IStorageItem item)
    {
        if (item is not null && item.IsOfType(StorageItemTypes.Folder))
        {
            return item is StorageFolder folder ? (BaseStorageFolder)folder : item as BaseStorageFolder;
        }

        return null;
    }
}
