﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace DesktopWidgets3.Files.Core.Services.SizeProvider;

public interface ISizeProvider : IDisposable
{
	event EventHandler<SizeChangedEventArgs> SizeChanged;

	Task CleanAsync();
	Task ClearAsync();

	Task UpdateAsync(string path, CancellationToken cancellationToken);
	bool TryGetSize(string path, out ulong size);
}
