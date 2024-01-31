﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;
using System.IO;

namespace Files.Core.Services.SizeProvider;

public class DrivesSizeProvider : ISizeProvider
{
	private readonly ConcurrentDictionary<string, ISizeProvider> providers = new();

	public event EventHandler<SizeChangedEventArgs>? SizeChanged;

	public async Task CleanAsync()
	{
		var currentDrives = DriveInfo.GetDrives().Select(x => x.Name).ToArray();
		var oldDriveNames = providers.Keys.Except(currentDrives).ToArray();

		foreach (var oldDriveName in oldDriveNames)
        {
            providers.TryRemove(oldDriveName, out _);
        }

        foreach (var provider in providers.Values)
        {
            await provider.CleanAsync();
        }
    }

	public async Task ClearAsync()
	{
		foreach (var provider in providers.Values)
        {
            await provider.ClearAsync();
        }

        providers.Clear();
	}

	/// <summary>
	/// Delegate the update to an instance of CachedSizeProvider.
	/// This method is reentrant (thread safe) to avoid having to await each result.
	/// </summary>
	public Task UpdateAsync(string path, CancellationToken cancellationToken)
	{
		var driveName = GetDriveName(path);
		var provider = providers.GetOrAdd(driveName, (key) =>
		{
			return CreateProvider();
		});
		return provider.UpdateAsync(path, cancellationToken);
	}

	public bool TryGetSize(string path, out ulong size)
	{
		var driveName = GetDriveName(path);
		if (!providers.TryGetValue(driveName, out var value))
		{
			size = 0;
			return false;
		}
		var provider = value;
		return provider.TryGetSize(path, out size);
	}

	private static string GetDriveName(string path) => Directory.GetDirectoryRoot(path);

	private ISizeProvider CreateProvider()
	{
		var provider = new CachedSizeProvider();
		provider.SizeChanged += Provider_SizeChanged;
		return provider;
	}

	private void Provider_SizeChanged(object? sender, SizeChangedEventArgs e)
		=> SizeChanged?.Invoke(this, e);

	public void Dispose()
	{
		foreach (var provider in providers.Values)
		{
			provider.SizeChanged -= Provider_SizeChanged;
			provider.Dispose();
		}
	}
}
