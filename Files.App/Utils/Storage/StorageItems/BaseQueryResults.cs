// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.App.Utils.Storage;

public partial class BaseStorageItemQueryResult(BaseStorageFolder folder, QueryOptions options)
{
    public BaseStorageFolder Folder { get; } = folder;
    public QueryOptions Options { get; } = options;

    public virtual IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxNumberOfItems)
	{
		return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
		{
			var items = (await GetItemsAsync()).Skip((int)startIndex).Take((int)Math.Min(maxNumberOfItems, int.MaxValue));
			return items.ToList();
		});
	}

	public virtual IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
	{
		return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
		{
			var items = await Folder.GetItemsAsync();
			var query = string.Join(' ', Options.ApplicationSearchFilter, Options.UserSearchFilter).Trim();
			if (!string.IsNullOrEmpty(query))
			{
                var spaceSplit = RegexHelpers.SpaceSplit().Split(query);
                foreach (var split in spaceSplit)
				{
					var colonSplit = split.Split(':');
					if (colonSplit.Length == 2)
					{
						if (colonSplit[0] == "System.FileName" || colonSplit[0] == "fileName" || colonSplit[0] == "name")
						{
							items = items.Where(x => Regex.IsMatch(x.Name, colonSplit[1].Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
						}
					}
					else
					{
						items = items.Where(x => Regex.IsMatch(x.Name, split.Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
					}
				}
			}
            return new List<IStorageItem>(items);
        });
	}

	public virtual StorageItemQueryResult ToStorageItemQueryResult() => null!;
}

public partial class BaseStorageFileQueryResult(BaseStorageFolder folder, QueryOptions options)
{
    public BaseStorageFolder Folder { get; } = folder;
    public QueryOptions Options { get; } = options;

    public virtual IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(uint startIndex, uint maxNumberOfItems)
	{
		return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
		{
			var items = (await GetFilesAsync()).Skip((int)startIndex).Take((int)Math.Min(maxNumberOfItems, int.MaxValue));
			return items.ToList();
		});
	}

	public virtual IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
	{
		return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
		{
			var items = await Folder.GetFilesAsync();
			var query = string.Join(' ', Options.ApplicationSearchFilter, Options.UserSearchFilter).Trim();
			if (!string.IsNullOrEmpty(query))
			{
                var spaceSplit = RegexHelpers.SpaceSplit().Split(query);
                foreach (var split in spaceSplit)
				{
					var colonSplit = split.Split(':');
					if (colonSplit.Length == 2)
					{
						if (colonSplit[0] == "System.FileName" || colonSplit[0] == "fileName" || colonSplit[0] == "name")
						{
							items = items.Where(x => Regex.IsMatch(x.Name, colonSplit[1].Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
						}
					}
					else
					{
						items = items.Where(x => Regex.IsMatch(x.Name, split.Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
					}
				}
			}
            return new List<BaseStorageFile>(items);
        });
	}

	public virtual StorageFileQueryResult ToStorageFileQueryResult() => null!;
}

public partial class BaseStorageFolderQueryResult(BaseStorageFolder folder, QueryOptions options)
{
    public BaseStorageFolder Folder { get; } = folder;
    public QueryOptions Options { get; } = options;

    public virtual IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(uint startIndex, uint maxNumberOfItems)
	{
		return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
		{
			var items = (await GetFoldersAsync()).Skip((int)startIndex).Take((int)Math.Min(maxNumberOfItems, int.MaxValue));
			return items.ToList();
		});
	}

	public virtual IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
	{
		return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
		{
			var items = await Folder.GetFoldersAsync();
			var query = string.Join(' ', Options.ApplicationSearchFilter, Options.UserSearchFilter).Trim();
			if (!string.IsNullOrEmpty(query))
			{
                var spaceSplit = RegexHelpers.SpaceSplit().Split(query);
                foreach (var split in spaceSplit)
				{
					var colonSplit = split.Split(':');
					if (colonSplit.Length == 2)
					{
						if (colonSplit[0] == "System.FileName" || colonSplit[0] == "fileName" || colonSplit[0] == "name")
						{
							items = items.Where(x => Regex.IsMatch(x.Name, colonSplit[1].Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
						}
					}
					else
					{
						items = items.Where(x => Regex.IsMatch(x.Name, split.Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
					}
				}
			}
            return new List<BaseStorageFolder>(items);
        });
	}

	public virtual StorageFolderQueryResult ToStorageFolderQueryResult() => null!;
}

public sealed class SystemStorageItemQueryResult(StorageItemQueryResult sfqr) : BaseStorageItemQueryResult(sfqr.Folder, sfqr.GetCurrentQueryOptions())
{
    private StorageItemQueryResult StorageItemQueryResult { get; } = sfqr;

    public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxNumberOfItems)
	{
		return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
		{
			var items = await StorageItemQueryResult.GetItemsAsync(startIndex, maxNumberOfItems);
			return items.Select(x => x is StorageFolder folder ? (IStorageItem)new SystemStorageFolder(folder) : new SystemStorageFile((StorageFile)x)).ToList();
		});
	}

	public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
	{
		return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
		{
			var items = await StorageItemQueryResult.GetItemsAsync();
			return items.Select(x => x is StorageFolder folder ? (IStorageItem)new SystemStorageFolder(folder) : new SystemStorageFile((StorageFile)x)).ToList();
		});
	}

	public override StorageItemQueryResult ToStorageItemQueryResult() => StorageItemQueryResult;
}

public sealed class SystemStorageFileQueryResult(StorageFileQueryResult sfqr) : BaseStorageFileQueryResult(sfqr.Folder, sfqr.GetCurrentQueryOptions())
{
    private StorageFileQueryResult StorageFileQueryResult { get; } = sfqr;

    public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(uint startIndex, uint maxNumberOfItems)
	{
		return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
		{
			var items = await StorageFileQueryResult.GetFilesAsync(startIndex, maxNumberOfItems);
			return items.Select(x => new SystemStorageFile(x)).ToList();
		});
	}

	public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
	{
		return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
		{
			var items = await StorageFileQueryResult.GetFilesAsync();
			return items.Select(x => new SystemStorageFile(x)).ToList();
		});
	}

	public override StorageFileQueryResult ToStorageFileQueryResult() => StorageFileQueryResult;
}

public sealed class SystemStorageFolderQueryResult(StorageFolderQueryResult sfqr) : BaseStorageFolderQueryResult(sfqr.Folder, sfqr.GetCurrentQueryOptions())
{
    private StorageFolderQueryResult StorageFolderQueryResult { get; } = sfqr;

    public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(uint startIndex, uint maxNumberOfItems)
	{
		return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
		{
			var items = await StorageFolderQueryResult.GetFoldersAsync(startIndex, maxNumberOfItems);
			return items.Select(x => new SystemStorageFolder(x)).ToList();
		});
	}

	public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
	{
		return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
		{
			var items = await StorageFolderQueryResult.GetFoldersAsync();
			return items.Select(x => new SystemStorageFolder(x)).ToList();
		});
	}

	public override StorageFolderQueryResult ToStorageFolderQueryResult() => StorageFolderQueryResult;
}
