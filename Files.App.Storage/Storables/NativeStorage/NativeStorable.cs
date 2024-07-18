// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Storage.Storables;

/// <inheritdoc cref="IStorable"/>
public abstract class NativeStorable<TStorage>(TStorage storage, string? name = null) : ILocatableStorable, INestedStorable
	where TStorage : FileSystemInfo
{
	protected readonly TStorage storage = storage;

    /// <inheritdoc/>
    public string Path { get; protected set; } = storage.FullName;

    /// <inheritdoc/>
    public string Name { get; protected set; } = name ?? storage.Name;

    /// <inheritdoc/>
    public virtual string Id { get; } = storage.FullName;

    /// <inheritdoc/>
    public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
	{
		var parent = Directory.GetParent(Path);
		if (parent is null)
        {
            return Task.FromResult<IFolder?>(null);
        }

        return Task.FromResult<IFolder?>(new NativeFolder(parent));
	}

	/// <summary>
	/// Formats a given <paramref name="path"/>.
	/// </summary>
	/// <param name="path">The path to format.</param>
	/// <returns>A formatted path.</returns>
	protected static string FormatPath(string path)
	{
		path = path.Replace("file:///", string.Empty);

		if ('/' != SystemIO.Path.DirectorySeparatorChar)
        {
            return path.Replace('/', SystemIO.Path.DirectorySeparatorChar);
        }

        if ('\\' != SystemIO.Path.DirectorySeparatorChar)
        {
            return path.Replace('\\', SystemIO.Path.DirectorySeparatorChar);
        }

        return path;
	}
}
