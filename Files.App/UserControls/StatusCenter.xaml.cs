// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls;

public sealed partial class StatusCenter : UserControl
{
	public StatusCenterViewModel ViewModel = null!;

	public StatusCenter()
	{
		InitializeComponent();
	}

    public void Initialize(IFolderViewViewModel folderViewModel)
    {
        ViewModel = folderViewModel.GetService<StatusCenterViewModel>();
    }

	private void CloseAllItemsButton_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.RemoveAllCompletedItems();
	}

	private void CloseItemButton_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button button && button.DataContext is StatusCenterItem item)
        {
            ViewModel.RemoveItem(item);
        }
    }

	private void ExpandCollapseChevronItemButton_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button button && button.DataContext is StatusCenterItem item)
		{
			var buttonAnimatedIcon = button.FindDescendant<AnimatedIcon>();

			if (buttonAnimatedIcon is not null)
            {
                AnimatedIcon.SetState(buttonAnimatedIcon, item.IsExpanded ? "NormalOff" : "NormalOn");
            }

            item.IsExpanded = !item.IsExpanded;
		}
	}
}
