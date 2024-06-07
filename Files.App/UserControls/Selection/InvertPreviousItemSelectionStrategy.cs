// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.App.UserControls.Selection;

internal sealed class InvertPreviousItemSelectionStrategy(ICollection<object> selectedItems, List<object> prevSelectedItems) : ItemSelectionStrategy(selectedItems)
{
	private readonly List<object> prevSelectedItems = prevSelectedItems;

    public override void HandleIntersectionWithItem(object item)
	{
		try
		{
			if (prevSelectedItems.Contains(item))
			{
				selectedItems.Remove(item);
			}
			else if (!selectedItems.Contains(item))
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
			// Restore selection on items not intersecting with the rectangle
			if (prevSelectedItems.Contains(item))
			{
				if (!selectedItems.Contains(item))
				{
					selectedItems.Add(item);
				}
			}
			else
			{
				selectedItems.Remove(item);
			}
		}
		catch (COMException) // List is being modified
		{
		}
	}
}