// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Storage.Storables;

/// <inheritdoc cref="IFile"/>
public class NativeFile(FileInfo fileInfo, string? name = null) : NativeStorable<FileInfo>(fileInfo, name), ILocatableFile, IModifiableFile, IFileExtended, INestedFile
{
    public NativeFile(string path, string? name = null)
		: this(new FileInfo(path), name)
	{
	}

	/// <inheritdoc/>
	public virtual Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
	{
		return OpenStreamAsync(access, FileShare.None, cancellationToken);
	}

	/// <inheritdoc/>
	public virtual Task<Stream> OpenStreamAsync(FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
	{
		var stream = File.Open(Path, FileMode.Open, access, share);
		return Task.FromResult<Stream>(stream);
	}
}
