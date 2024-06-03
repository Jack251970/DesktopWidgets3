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

    // Dependency injections

    public IUserSettingsService UserSettingsService { get; set; } = null!;
	public IQuickAccessService QuickAccessService { get; } = DependencyExtensions.GetService<IQuickAccessService>();
	public IStorageService StorageService { get; } = DependencyExtensions.GetService<IStorageService>();

    // Fields

    protected string? FlyoutItemPath;

    // Commands

    public ICommand? RemoveRecentItemCommand { get; protected set; }
    public ICommand? ClearAllItemsCommand { get; protected set; }
    public ICommand? OpenFileLocationCommand { get; protected set; }
    public ICommand? OpenInNewTabCommand { get; protected set; }
    public ICommand? OpenInNewWindowCommand { get; protected set; }
    public ICommand? OpenPropertiesCommand { get; protected set; }
    public ICommand? PinToFavoritesCommand { get; protected set; }
    public ICommand? UnpinFromFavoritesCommand { get; protected set; }

    // Events

    public static event EventHandler<WidgetsRightClickedItemChangedEventArgs>? RightClickedItemChanged;  // TODO: Check if can be static.

    // Abstract methods

    public abstract List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false);

    // Initialize method

    // CHANGE: Initialize folder view view model and related services.
    protected void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
        UserSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
    }

    // Event methods

    public void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Ensure values are not null
        if (sender is not Button widgetCardItem ||
            widgetCardItem.DataContext is not WidgetCardItem item)
        {
            return;
        }

        // Create a new Flyout
        var itemContextMenuFlyout = new CommandBarFlyout()
        {
            Placement = FlyoutPlacementMode.Full
        };

        // Hook events
        itemContextMenuFlyout.Opening += (sender, e) => FolderViewViewModel.LastOpenedFlyout = sender as CommandBarFlyout;
        itemContextMenuFlyout.Closed += (sender, e) => OnRightClickedItemChanged(null, null);

        FlyoutItemPath = item.Path;

        // Notify of the change on right clicked item
        OnRightClickedItemChanged(item, itemContextMenuFlyout);

        // Get items for the flyout
        var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
        var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

        // Set max width of the flyout
        secondaryElements
            .OfType<FrameworkElement>()
            .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

        // Add menu items to the secondary flyout
        secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

        // Show the flyout
        itemContextMenuFlyout.ShowAt(widgetCardItem, new() { Position = e.GetPosition(widgetCardItem) });

        // Load shell menu items
        _ = ShellContextmenuHelper.LoadShellMenuItemsAsync(FolderViewViewModel, FlyoutItemPath, itemContextMenuFlyout);

        e.Handled = true;
    }

    // Command methods

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

    protected void OnRightClickedItemChanged(WidgetCardItem? item, CommandBarFlyout? flyout)
    {
        RightClickedItemChanged?.Invoke(this, new WidgetsRightClickedItemChangedEventArgs(item, flyout));
    }
}
