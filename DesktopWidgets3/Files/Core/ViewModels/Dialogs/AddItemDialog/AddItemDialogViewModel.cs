﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Files.Core.Data.Enums;
using DesktopWidgets3.Files.Core.Data.Items;
using DesktopWidgets3.Files.Core.Data.Models;
using DesktopWidgets3.Files.Core.Services;
using DesktopWidgets3.Files.Shared.Utils;
using DesktopWidgets3.Helpers;

namespace DesktopWidgets3.Files.Core.ViewModels.Dialogs.AddItemDialog;

public sealed class AddItemDialogViewModel : ObservableObject
{
	private readonly IImageService _imagingService;

	public ObservableCollection<AddItemDialogListItemViewModel> AddItemsList { get; }

	public AddItemDialogResultModel ResultType { get; set; }

	public AddItemDialogViewModel()
	{
		// Dependency injection
		_imagingService = DesktopWidgets3.App.GetService<IImageService>();

		// Initialize
		AddItemsList = new();
		ResultType = new()
		{
			ItemType = AddItemDialogItemType.Cancel
		};
	}

	public async Task AddItemsToListAsync(IEnumerable<ShellNewEntry> itemTypes)
	{
		AddItemsList.Clear();

		AddItemsList.Add(new()
		{
			Header = "Folder".GetLocalized(),
			SubHeader = "AddDialogListFolderSubHeader".GetLocalized(),
			Glyph = "\xE838",
			IsItemEnabled = true,
			ItemResult = new()
			{
				ItemType = AddItemDialogItemType.Folder
			}
		});

		AddItemsList.Add(new()
		{
			Header = "File".GetLocalized(),
			SubHeader = "AddDialogListFileSubHeader".GetLocalized(),
			Glyph = "\xE8A5",
			IsItemEnabled = true,
			ItemResult = new()
			{
				ItemType = AddItemDialogItemType.File,
				ItemInfo = null
			}
		});

		AddItemsList.Add(new()
		{
			Header = "Shortcut".GetLocalized(),
			SubHeader = "AddDialogListShortcutSubHeader".GetLocalized(),
			Glyph = "\uE71B",
			IsItemEnabled = true,
			ItemResult = new()
			{
				ItemType = AddItemDialogItemType.Shortcut,
				ItemInfo = null
			}
		});

        if (itemTypes is null)
        {
            return;
        }

        foreach (var itemType in itemTypes)
		{
			IImage? imageModel = null;

			if (!string.IsNullOrEmpty(itemType.IconBase64))
			{
				var bitmapData = Convert.FromBase64String(itemType.IconBase64);
				imageModel = await _imagingService.GetImageModelFromDataAsync(bitmapData);
			}

			AddItemsList.Add(new()
			{
				Header = itemType.Name,
				SubHeader = itemType.Extension,
				Glyph = imageModel is not null ? null : "\xE8A5",
				Icon = imageModel,
				IsItemEnabled = true,
				ItemResult = new()
				{
					ItemType = AddItemDialogItemType.File,
					ItemInfo = itemType
				}
			});
		}
	}
}
