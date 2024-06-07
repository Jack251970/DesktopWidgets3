// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Data.Models;

public sealed class ContextMenuFlyoutItemViewModelBuilder(IRichCommand command)
{
	private static readonly ContextMenuFlyoutItemViewModel none = new()
	{
		ShowItem = false,
		IsHidden = true,
	};

	private readonly IRichCommand command = command;

	private bool? isVisible = null;
	public bool IsVisible
	{
		get => isVisible ?? command.IsExecutable;
		init => isVisible = value;
	}

	public bool IsPrimary { get; init; } = false;
	public bool IsToggle { get; init; } = false;

    public object Tag { get; init; } = null!;

	public bool ShowOnShift { get; init; } = false;

	public List<ContextMenuFlyoutItemViewModel>? Items { get; init; } = null;

    public ContextMenuFlyoutItemViewModel Build()
	{
		if (isVisible is false)
        {
            return none;
        }

        var isExecutable = command.IsExecutable;

		if (isVisible is null && !isExecutable)
        {
            return none;
        }

        var type = IsToggle ? ContextMenuFlyoutItemType.Toggle : ContextMenuFlyoutItemType.Item;

		var viewModel = new ContextMenuFlyoutItemViewModel
		{
			Text = command.Label,
			Tag = Tag,
			Command = command,
			IsEnabled = isExecutable,
			IsChecked = command.IsOn,
			IsPrimary = IsPrimary,
			Items = Items!,
			ItemType = type,
			ShowItem = true,
			ShowOnShift = ShowOnShift,
			ShowInRecycleBin = true,
			ShowInSearchPage = true,
			ShowInFtpPage = true,
			ShowInZipPage = true,
		};

		var glyph = command.Glyph;
		if (!string.IsNullOrEmpty(glyph.OpacityStyle))
		{
			viewModel.OpacityIcon = new OpacityIconModel
			{
				OpacityIconStyle = glyph.OpacityStyle,
			};
		}
		else
		{
			viewModel.Glyph = glyph.BaseGlyph;
			viewModel.GlyphFontFamilyName = glyph.FontFamily;
		}

		if (command.HotKeys.Length > 0 &&
			!(command.HotKeys[0].Key is Keys.Enter &&
			command.HotKeys[0].Modifier is KeyModifiers.None))
		{
			viewModel.KeyboardAccelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator()
			{
				Key = (VirtualKey)command.HotKeys[0].Key,
				Modifiers = (VirtualKeyModifiers)command.HotKeys[0].Modifier
			};
			viewModel.KeyboardAcceleratorTextOverride = command.HotKeys[0].LocalizedLabel;
		}

		return viewModel;
	}
}
