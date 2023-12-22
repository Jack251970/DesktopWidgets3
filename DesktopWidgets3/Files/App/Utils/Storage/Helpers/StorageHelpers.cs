// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils;
using Files.App.Utils.Storage;
using Files.App.Utils.Storage.Helpers;
using Files.Core.Data.Enums;
using Windows.Storage;

namespace Files.App.Helpers;

/// <summary>
/// <see cref="IStorageItem"/> related Helpers
/// </summary>
public static class StorageHelpers
{
    public static IStorageItemWithPath FromPathAndType(string customPath, FilesystemItemType? itemType)
    {
        return (itemType == FilesystemItemType.File) ?
                new StorageFileWithPath(null!, customPath) :
                new StorageFolderWithPath(null!, customPath);
    }

    public static IStorageItemWithPath FromStorageItem(this IStorageItem item, string customPath = null!, FilesystemItemType? itemType = null)
    {
        if (item is null)
        {
            return FromPathAndType(customPath, itemType);
        }
        else if (item.IsOfType(StorageItemTypes.File))
        {
            return new StorageFileWithPath(item.AsBaseStorageFile()!, string.IsNullOrEmpty(item.Path) ? customPath : item.Path);
        }
        else if (item.IsOfType(StorageItemTypes.Folder))
        {
            return new StorageFolderWithPath(item.AsBaseStorageFolder()!, string.IsNullOrEmpty(item.Path) ? customPath : item.Path);
        }
        return null!;
    }

    public static async Task<FilesystemResult<IStorageItem>> ToStorageItemResult(this IStorageItemWithPath item)
    {
        var returnedItem = new FilesystemResult<IStorageItem>(null!, FileSystemStatusCode.Generic);
        var rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(item.Path));
        if (!string.IsNullOrEmpty(item.Path))
        {
            returnedItem = (item.ItemType == FilesystemItemType.File) ?
                ToType<IStorageItem, BaseStorageFile>(
                    await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.Path, rootItem))) :
                ToType<IStorageItem, BaseStorageFolder>(
                    await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(item.Path, rootItem)));
        }
        if (returnedItem.Result is null && item.Item is not null)
        {
            returnedItem = new FilesystemResult<IStorageItem>(item.Item, FileSystemStatusCode.Success);
        }
        if (returnedItem.Result is IPasswordProtectedItem ppid && item.Item is IPasswordProtectedItem ppis)
        {
            ppid.Credentials = ppis.Credentials;
        }
        return returnedItem;
    }

    public static FilesystemResult<T> ToType<T, V>(FilesystemResult<V> result) where T : class
    {
        return new FilesystemResult<T>((result.Result as T)!, result.ErrorCode);
    }
}
