// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services;

/// <inheritdoc cref="IAddItemService"/>
// TODO: Change to internal.
public sealed class AddItemService : IAddItemService
{
	private List<ShellNewEntry> _cached = null!;

    // TODO: Initalize this service in AppLifecycleService.cs.
	public async Task InitializeAsync()
	{
		_cached = await ShellNewEntryExtensions.GetNewContextMenuEntries();
	}

	public List<ShellNewEntry> GetEntries()
	{
		return _cached;
	}
}

