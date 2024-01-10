// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Extensions;
using Files.Core.Data.Items;
using Files.Core.Services;

namespace Files.App.Services;

/// <inheritdoc cref="IAddItemService"/>
internal sealed class AddItemService : IAddItemService
{
	private List<ShellNewEntry> _cached = null!;

    private bool _initialized = false;

	public async Task InitializeAsync()
	{
        if (!_initialized)
        {
            _cached = await ShellNewEntryExtensions.GetNewContextMenuEntries();

            _initialized = true;
        }
	}

	public List<ShellNewEntry> GetEntries()
	{
		return _cached;
	}
}
