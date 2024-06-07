// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.App.UserControls.Selection;

public sealed class IgnorePreviousItemSelectionStrategy(ICollection<object> selectedItems) : ItemSelectionStrategy(selectedItems)
{
    public override void HandleIntersectionWithItem(object item)
	{
		try
		{
			// Select item intersection with the rectangle
			if (!selectedItems.Contains(item))
			{
				selectedItems.Add(item);
			}
		}
		catch (COMException) // List is being modified
		{
		}
	}

	public override void HandleNoIntersectionWithItem(object item)
	{
		try
		{
			selectedItems.Remove(item);
		}
		catch (COMException) // List is being modified
		{
		}
	}

	public override void StartSelection()
	{
		selectedItems.Clear();
	}

	public override void HandleNoItemSelected()
	{
		selectedItems.Clear();
	}
}