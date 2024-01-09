// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.Core.Storage;
using DesktopWidgets3.Files.Core.Storage.LocatableStorage;
using DesktopWidgets3.Files.Core.Storage.NestedStorage;
using FluentFTP;

namespace DesktopWidgets3.Files.App.Storage.FtpStorage;

public abstract class FtpStorable : ILocatableStorable, INestedStorable
{
	/// <inheritdoc/>
	public virtual string Path { get; protected set; }

	/// <inheritdoc/>
	public virtual string Name { get; protected set; }

	/// <inheritdoc/>
	public virtual string Id { get; }

	/// <summary>
	/// Gets the parent folder of the storable, if any.
	/// </summary>
	protected virtual IFolder? Parent { get; }

	protected internal FtpStorable(string path, string name, IFolder? parent)
	{
		Path = FtpHelpers.GetFtpPath(path);
		Name = name;
		Id = Path;
		Parent = parent;
	}

	/// <inheritdoc/>
	public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(Parent);
	}

	protected AsyncFtpClient GetFtpClient()
	{
		return FtpHelpers.GetFtpClient(Path);
	}
}
