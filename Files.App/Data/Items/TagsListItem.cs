// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items;

public class TagsListItem
{
	public bool IsTag
		=> this is TagItem;

	public TagItem? AsTag
		=> this as TagItem;

	public bool IsFlyout
		=> this is FlyoutItem;

	public FlyoutItem? AsFlyout
		=> this as FlyoutItem;
}

public sealed class TagItem(TagViewModel tag) : TagsListItem
{
    public TagViewModel Tag { get; set; } = tag;
}

public sealed class FlyoutItem(MenuFlyout flyout) : TagsListItem
{
    public MenuFlyout Flyout { get; set; } = flyout;
}
