// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.App.Helpers;
using DesktopWidgets3.Files.App.Utils.Shell;
using DesktopWidgets3.Files.App.Utils.Storage;
using DesktopWidgets3.Files.App.Utils.Storage.Helpers;
using DesktopWidgets3.Files.Core.Data.Items;
using DesktopWidgets3.Files.Shared.Extensions;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Windows.Storage;

namespace DesktopWidgets3.Files.App.Extensions;

public static class ShellNewEntryExtensions
{
	public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
	{
		var shellEntryList = new List<ShellNewEntry>();

		var entries = await SafetyExtensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntries());
		if (entries is not null)
		{
			shellEntryList.AddRange(entries);
		}

		return shellEntryList;
	}

	public static async Task<ShellNewEntry?> GetNewContextMenuEntryForType(string extension)
	{
		return await SafetyExtensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntryForType(extension));
	}

	public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, string filePath, FolderViewViewModel viewModel)
	{
		var parentFolder = await viewModel.FileSystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(filePath));
		if (parentFolder)
		{
			return await Create(shellEntry, parentFolder, filePath);
		}

		return new FilesystemResult<BaseStorageFile>(null!, parentFolder.ErrorCode);
	}

	public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, BaseStorageFolder parentFolder, string filePath)
	{
		FilesystemResult<BaseStorageFile> createdFile = null!;
		var fileName = Path.GetFileName(filePath);

		if (shellEntry.Template is null)
		{
			createdFile = await FilesystemTasks.Wrap(() => parentFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName).AsTask());
		}
		else
		{
			createdFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(shellEntry.Template))
				.OnSuccess(t => t.CopyAsync(parentFolder, fileName, NameCollisionOption.GenerateUniqueName).AsTask());
		}

		if (createdFile && shellEntry.Data is not null)
		{
			// Calls unsupported OpenTransactedWriteAsync
			//await FileIO.WriteBytesAsync(createdFile.Result, shellEntry.Data);

			await createdFile.Result.WriteBytesAsync(shellEntry.Data);
		}

		return createdFile;
	}
}
