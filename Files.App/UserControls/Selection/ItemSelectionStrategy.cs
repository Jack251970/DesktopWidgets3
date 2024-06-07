// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.UserControls.Selection;

public abstract class ItemSelectionStrategy(ICollection<object> selectedItems)
{
	protected readonly ICollection<object> selectedItems = selectedItems;

    public abstract void HandleIntersectionWithItem(object item);

	public abstract void HandleNoIntersectionWithItem(object item);

	public virtual void StartSelection()
	{
	}

	public virtual void HandleNoItemSelected()
	{
	}
}