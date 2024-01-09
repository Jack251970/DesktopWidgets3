// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace DesktopWidgets3.Files.App.Utils.Storage;

internal interface IStorageCacheController
{
    public ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken? cancellationToken);

    public ValueTask SaveFileDisplayNameToCache(string path, string displayName);
}