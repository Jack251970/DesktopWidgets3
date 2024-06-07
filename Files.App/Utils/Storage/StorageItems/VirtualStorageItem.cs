// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage;

/// <summary>
/// Implements IStorageItem, allowing us to get an instance of IStorageItem for a ListedItem
/// representing a standard filesystem item. As such, VirtualStorageItem does not support hidden,
/// shortcut, or link items.
/// </summary>
public sealed class VirtualStorageItem : IStorageItem
{
	private static BasicProperties props = null!;

	public Windows.Storage.FileAttributes Attributes { get; init; }

	public DateTimeOffset DateCreated { get; init; }

    public string Name { get; init; } = null!;

	public string Path { get; init; } = null!;

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
        var findInfoLevel = Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic;
        var additionalFlags = Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH;
        var hFile = Win32PInvoke.FindFirstFileExFromApp(path, findInfoLevel, out var findData, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
        if (hFile.ToInt64() != -1)
		{
			// https://learn.microsoft.com/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4
			var isReparsePoint = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint;
            var isSymlink = isReparsePoint && findData.dwReserved0 == Win32PInvoke.IO_REPARSE_TAG_SYMLINK;
            var isHidden = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden;
			var isDirectory = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Directory) == System.IO.FileAttributes.Directory;

			if (!(isHidden && isSymlink))
			{
				DateTime itemCreatedDate;

				try
				{
                    Win32PInvoke.FileTimeToSystemTime(ref findData.ftCreationTime, out var systemCreatedDateOutput);
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

            Win32PInvoke.FindClose(hFile);
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
