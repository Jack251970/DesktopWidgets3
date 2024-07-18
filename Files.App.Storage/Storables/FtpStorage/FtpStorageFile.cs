// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Storage.Storables;

public sealed class FtpStorageFile(string path, string name, IFolder? parent) : FtpStorable(path, name, parent), IModifiableFile, ILocatableFile, INestedFile
{
    /// <inheritdoc/>
    public async Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
	{
		using var ftpClient = GetFtpClient();
		await ftpClient.EnsureConnectedAsync(cancellationToken);

		if (access.HasFlag(FileAccess.Write))
        {
            return await ftpClient.OpenWrite(Path, token: cancellationToken);
        }
        else if (access.HasFlag(FileAccess.Read))
        {
            return await ftpClient.OpenRead(Path, token: cancellationToken);
        }
		else
        {
            throw new ArgumentException($"Invalid {nameof(access)} flag.");
        }
	}
}
