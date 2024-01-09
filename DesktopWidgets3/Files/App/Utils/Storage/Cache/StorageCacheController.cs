﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.App.Utils.Storage;
using System.Collections.Concurrent;

internal class StorageCacheController : IStorageCacheController
{
    private static StorageCacheController instance = null!;

    private readonly ConcurrentDictionary<string, string> fileNamesCache = new();

    private StorageCacheController()
    {
    }

    public static StorageCacheController GetInstance()
    {
        return instance ??= new StorageCacheController();
    }

    public ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken? cancellationToken)
    {
        return fileNamesCache.TryGetValue(path, out var displayName) ? ValueTask.FromResult(displayName) : ValueTask.FromResult(string.Empty);
    }

    public ValueTask SaveFileDisplayNameToCache(string path, string displayName)
    {
        if (displayName is null)
        {
            fileNamesCache.TryRemove(path, out _);
        }

        fileNamesCache[path] = displayName!;
        return ValueTask.CompletedTask;
    }
}
