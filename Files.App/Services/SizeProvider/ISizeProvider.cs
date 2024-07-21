// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.SizeProvider;

public interface ISizeProvider : IDisposable
{
    void Initialize(IFolderViewViewModel folderViewViewModel);

    event EventHandler<SizeChangedEventArgs> SizeChanged;

	Task CleanAsync();
	Task ClearAsync();

	Task UpdateAsync(string path, CancellationToken cancellationToken);
	bool TryGetSize(string path, out ulong size);
}
