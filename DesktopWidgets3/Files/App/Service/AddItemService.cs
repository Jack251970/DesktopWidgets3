// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.App.Extensions;
using DesktopWidgets3.Files.Core.Data.Items;
using DesktopWidgets3.Files.Core.Services;

namespace DesktopWidgets3.Files.App.Services;

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
