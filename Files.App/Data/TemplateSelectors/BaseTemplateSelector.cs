// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.TemplateSelectors;

// TODO: change to internal.
public abstract class BaseTemplateSelector<TItem> : DataTemplateSelector
{
	protected override sealed DataTemplate SelectTemplateCore(object item)
	{
		return SelectTemplateCore((TItem?)item);
	}

	protected override sealed DataTemplate SelectTemplateCore(object item, DependencyObject container)
	{
		return SelectTemplateCore((TItem?)item, container);
	}

	protected virtual DataTemplate SelectTemplateCore(TItem? item)
	{
		return base.SelectTemplateCore(item);
	}

	protected virtual DataTemplate SelectTemplateCore(TItem? item, DependencyObject container)
	{
		return base.SelectTemplateCore(item, container);
	}
}
