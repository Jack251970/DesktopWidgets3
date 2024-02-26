// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Files.Core.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;

namespace Files.App.UserControls.Widgets;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public abstract class HomePageWidget : UserControl
{
    protected IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    public IUserSettingsService UserSettingsService { get; set; } = null!;
	public IQuickAccessService QuickAccessService { get; } = DependencyExtensions.GetService<IQuickAccessService>();
	public IStorageService StorageService { get; } = DependencyExtensions.GetService<IStorageService>();

	public ICommand RemoveRecentItemCommand;
	public ICommand ClearAllItemsCommand;
	public ICommand OpenFileLocationCommand;
	public ICommand OpenInNewTabCommand;
	public ICommand OpenInNewWindowCommand;
	public ICommand OpenPropertiesCommand;
	public ICommand PinToFavoritesCommand;
	public ICommand UnpinFromFavoritesCommand;

	protected CommandBarFlyout ItemContextMenuFlyout;
	protected string FlyouItemPath;

	public abstract List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false);

    // CHANGE: Initialize folder view view model and related services.
    protected void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
        UserSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
    }

    public void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
		itemContextMenuFlyout.Opening += (sender, e) => FolderViewViewModel.LastOpenedFlyout = sender as CommandBarFlyout;
		if (sender is not Button widgetCardItem || widgetCardItem.DataContext is not WidgetCardItem item)
        {
            return;
        }

        var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
		var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

		secondaryElements.OfType<FrameworkElement>()
							.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

		secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);
		ItemContextMenuFlyout = itemContextMenuFlyout;
		FlyouItemPath = item.Path;
		ItemContextMenuFlyout.Opened += ItemContextMenuFlyout_Opened;
		itemContextMenuFlyout.ShowAt(widgetCardItem, new FlyoutShowOptions { Position = e.GetPosition(widgetCardItem) });

		e.Handled = true;
	}

	private async void ItemContextMenuFlyout_Opened(object? sender, object e)
	{
		ItemContextMenuFlyout.Opened -= ItemContextMenuFlyout_Opened;
		await ShellContextmenuHelper.LoadShellMenuItemsAsync(FolderViewViewModel, FlyouItemPath, ItemContextMenuFlyout);
	}

	public async Task OpenInNewTabAsync(WidgetCardItem? item)
	{
		await NavigationHelpers.OpenPathInNewTab(FolderViewViewModel, item!.Path);
	}

	public static async Task OpenInNewWindowAsync(WidgetCardItem? item)
	{
		await NavigationHelpers.OpenPathInNewWindowAsync(item!.Path);
	}

	public async virtual Task PinToFavoritesAsync(WidgetCardItem? item)
	{
		await QuickAccessService.PinToSidebarAsync(item!.Path);
	}

	public async virtual Task UnpinFromFavoritesAsync(WidgetCardItem? item)
	{
		await QuickAccessService.UnpinFromSidebarAsync(item!.Path);
	}

}
