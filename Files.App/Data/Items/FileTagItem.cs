// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Items;

public sealed class FileTagItem : ObservableObject, INavigationControlItem
{
	public string Text { get; set; } = null!;

    private string path = null!;
	public string Path
	{
		get => path;
		set
		{
			path = value;
			OnPropertyChanged(nameof(IconSource));
			OnPropertyChanged(nameof(ToolTip));
		}
	}

	public string ToolTipText { get; private set; } = null!;

    public SectionType Section { get; set; }

	public ContextMenuOptions MenuOptions { get; set; } = null!;

    public NavigationControlItemType ItemType
		=> NavigationControlItemType.FileTag;

	public int CompareTo(INavigationControlItem? other)
		=> Text.CompareTo(other?.Text);

    public TagViewModel FileTag { get; set; } = null!;

	public object? Children => null;

    public IconSource? IconSource => new PathIconSource()
    {
        Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["ColorIconFilledTag"]),
        Foreground = new SolidColorBrush(FileTag.Color.ToColor())
    };

    public object ToolTip => Text;

	public bool IsExpanded { get => false; set { } }
}
