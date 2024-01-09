// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices.WindowsRuntime;
using DesktopWidgets3.Files.App.Utils;
using DesktopWidgets3.Files.App.Helpers;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using static DesktopWidgets3.Files.Core.Helpers.NativeFindStorageItemHelper;

namespace DesktopWidgets3.Files.App.Utils.Storage;

/// <summary>
/// Implements IStorageItem, allowing us to get an instance of IStorageItem for a ListedItem
/// representing a standard filesystem item. As such, VirtualStorageItem does not support hidden,
/// shortcut, or link items.
/// </summary>
public class VirtualStorageItem : IStorageItem
{
    private static BasicProperties props = null!;

    public Windows.Storage.FileAttributes Attributes
    {
        get; init;
    }

    public DateTimeOffset DateCreated
    {
        get; init;
    }

    public string Name
    {
        get; init;
    }

    public string Path
    {
        get; init;
    }

    private VirtualStorageItem()
    {
    }

    public static VirtualStorageItem FromListedItem(ListedItem item)
    {
        return new VirtualStorageItem()
        {
            Name = item.ItemNameRaw,
            Path = item.ItemPath,
            DateCreated = item.ItemDateCreatedReal,
            Attributes = item.IsArchive || item.PrimaryItemAttribute == StorageItemTypes.File ? Windows.Storage.FileAttributes.Normal : Windows.Storage.FileAttributes.Directory
        };
    }

    public static VirtualStorageItem FromPath(string path)
    {
        var findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
        var additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
        var hFile = FindFirstFileExFromApp(path, findInfoLevel, out var findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
        if (hFile.ToInt64() != -1)
        {
            // https://learn.microsoft.com/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4
            var isReparsePoint = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint;
            var isSymlink = isReparsePoint && findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK;
            var isHidden = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden;
            var isDirectory = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory;

            if (!(isHidden && isSymlink))
            {
                DateTime itemCreatedDate;

                try
                {
                    FileTimeToSystemTime(ref findData.ftCreationTime, out var systemCreatedDateOutput);
                    itemCreatedDate = systemCreatedDateOutput.ToDateTime();
                }
                catch (ArgumentException)
                {
                    // Invalid date means invalid findData, do not add to list
                    return null!;
                }

                return new VirtualStorageItem()
                {
                    Name = findData.cFileName,
                    Path = path,
                    DateCreated = itemCreatedDate,
                    Attributes = isDirectory ? Windows.Storage.FileAttributes.Directory : Windows.Storage.FileAttributes.Normal
                };
            }

            FindClose(hFile);
        }

        return null!;
    }

    private async void StreamedFileWriterAsync(StreamedFileDataRequest request)
    {
        try
        {
            using (var stream = request.AsStreamForWrite())
            {
                await stream.FlushAsync();
            }
            request.Dispose();
        }
        catch (Exception)
        {
            request.FailAndClose(StreamedFileFailureMode.Incomplete);
        }
    }

    public IAsyncAction RenameAsync(string desiredName)
    {
        throw new NotImplementedException();
    }

    public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
    {
        throw new NotImplementedException();
    }

    public IAsyncAction DeleteAsync()
    {
        throw new NotImplementedException();
    }

    public IAsyncAction DeleteAsync(StorageDeleteOption option)
    {
        throw new NotImplementedException();
    }

    public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
    {
        return AsyncInfo.Run(async (cancellationToken) =>
        {
            async Task<BasicProperties> GetFakeBasicProperties()
            {
                var streamedFile = await StorageFile.CreateStreamedFileAsync(Name, StreamedFileWriterAsync, null);
                return await streamedFile.GetBasicPropertiesAsync();
            }
            return props ??= await GetFakeBasicProperties();
        });
    }

    public bool IsOfType(StorageItemTypes type)
    {
        return Attributes.HasFlag(Windows.Storage.FileAttributes.Directory) ? type == StorageItemTypes.Folder : type == StorageItemTypes.File;
    }
}
