// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.EventArguments;

public sealed class WidgetsRightClickedItemChangedEventArgs(WidgetCardItem? item = null, CommandBarFlyout? flyout = null)
{
    public WidgetCardItem? Item { get; set; } = item;

    public CommandBarFlyout? Flyout { get; set; } = flyout;
}
