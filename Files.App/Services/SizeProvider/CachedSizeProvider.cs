﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;
using System.IO;

namespace Files.App.Services.SizeProvider;

public sealed class CachedSizeProvider : ISizeProvider
{
	private readonly ConcurrentDictionary<string, ulong> sizes = new();

	public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    public void Initialize(IFolderViewViewModel folderViewViewModel) => throw new NotImplementedException();

    public Task CleanAsync() => Task.CompletedTask;

	public Task ClearAsync()
	{
		sizes.Clear();
		return Task.CompletedTask;
	}

	public async Task UpdateAsync(string path, CancellationToken cancellationToken)
	{
		await Task.Yield();
		if (sizes.TryGetValue(path, out var cachedSize))
		{
			RaiseSizeChanged(path, cachedSize, SizeChangedValueState.Final);
		}
		else
		{
			RaiseSizeChanged(path, 0, SizeChangedValueState.None);
		}

		var size = await Calculate(path);

		sizes[path] = size;
		RaiseSizeChanged(path, size, SizeChangedValueState.Final);

		async Task<ulong> Calculate(string path, int level = 0)
		{
			if (string.IsNullOrEmpty(path))
			{
				return 0;
			}

			var hFile = Win32PInvoke.FindFirstFileExFromApp($"{path}{Path.DirectorySeparatorChar}*.*", Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
				out var findData, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH);

			ulong size = 0;
			ulong localSize = 0;
			var localPath = string.Empty;

			if (hFile.ToInt64() is not -1)
			{
				do
				{
					if (((FileAttributes)findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    {
                        // Skip symbolic links and junctions
                        continue;
                    }

                    var isDirectory = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) is FileAttributes.Directory;
					if (!isDirectory)
					{
						size += (ulong)findData.GetSize();
					}
					else if (findData.cFileName is not "." and not "..")
					{
						localPath = Path.Combine(path, findData.cFileName);
						localSize = await Calculate(localPath, level + 1);
						size += localSize;
					}

					if (level <= 3)
					{
						await Task.Yield();
						sizes[localPath] = localSize;
					}
					if (level is 0)
					{
						RaiseSizeChanged(path, size, SizeChangedValueState.Intermediate);
					}

					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}
				} while (Win32PInvoke.FindNextFile(hFile, out findData));
				Win32PInvoke.FindClose(hFile);
			}
			return size;
		}
	}

	public bool TryGetSize(string path, out ulong size) => sizes.TryGetValue(path, out size);

	public void Dispose() { }

	private void RaiseSizeChanged(string path, ulong newSize, SizeChangedValueState valueState)
		=> SizeChanged?.Invoke(this, new SizeChangedEventArgs(path, newSize, valueState));
}
