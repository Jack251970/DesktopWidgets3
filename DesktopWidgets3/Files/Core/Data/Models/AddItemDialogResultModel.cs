﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.Core.Data.Enums;
using DesktopWidgets3.Files.Core.Data.Items;

namespace DesktopWidgets3.Files.Core.Data.Models;

/// <summary>
/// Represents a model for AddItemDialog result.
/// </summary>
public sealed class AddItemDialogResultModel
{
	/// <summary>
	/// Gets or sets item type that is added.
	/// </summary>
	public AddItemDialogItemType ItemType { get; set; }

	/// <summary>
	/// Gets or sets added item information.
	/// </summary>
	public ShellNewEntry? ItemInfo { get; set; }
}
