// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage;

// TODO: change to internal.
public interface IStorageCacheController
{
	public ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken);

	public ValueTask SaveFileDisplayNameToCache(string path, string displayName);
}
