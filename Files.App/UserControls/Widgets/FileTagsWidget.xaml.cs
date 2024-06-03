﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.IO;
using System.Windows.Input;
using Windows.Storage;

namespace Files.App.UserControls.Widgets;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public sealed partial class FileTagsWidget : HomePageWidget, IWidgetItem
{
	private IUserSettingsService userSettingsService;
    private IHomePageContext HomePageContext { get; } = DependencyExtensions.GetService<IHomePageContext>();

    public FileTagsWidgetViewModel ViewModel
	{
		get => (FileTagsWidgetViewModel)DataContext;
		set => DataContext = value;
	}

	public IShellPage AppInstance;

	public Func<string, Task>? OpenAction { get; set; }

	public delegate void FileTagsOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
	public delegate void FileTagsNewPaneInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);

    public static event EventHandler<IEnumerable<FileTagsItemViewModel>>? SelectedTaggedItemsChanged;  // TODO: Check if can be static.
    public event FileTagsOpenLocationInvokedEventHandler FileTagsOpenLocationInvoked;
	public event FileTagsNewPaneInvokedEventHandler FileTagsNewPaneInvoked;

	public string WidgetName => nameof(FileTagsWidget);

	public string WidgetHeader => "FileTags".GetLocalizedResource();

	public string AutomationProperties => "FileTags".GetLocalizedResource();

	public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;

	public bool ShowMenuFlyout => false;

	public MenuFlyoutItem MenuFlyoutItem => null!;

	private readonly ICommand OpenInNewPaneCommand;

	public FileTagsWidget()
	{
		/*userSettingsService = DependencyExtensions.GetService<IUserSettingsService>();*/

		InitializeComponent();

		// Second function is layered on top to ensure that OpenPath function is late initialized and a null reference is not passed-in
		// See FileTagItemViewModel._openAction for more information
		ViewModel = new(x => OpenAction!(x));
		OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewTabAsync);
		OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewWindowAsync);
		OpenFileLocationCommand = new RelayCommand<WidgetCardItem>(OpenFileLocation);
		OpenInNewPaneCommand = new RelayCommand<WidgetCardItem>(OpenInNewPane);
		PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(PinToFavoritesAsync);
		UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(UnpinFromFavoritesAsync);
		OpenPropertiesCommand = new RelayCommand<WidgetCardItem>(OpenProperties);
	}

    public new void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        base.Initialize(folderViewViewModel);

        userSettingsService = UserSettingsService;
    }

    private void OpenProperties(WidgetCardItem? item)
	{
        if (!HomePageContext.IsAnyItemRightClicked)
        {
            return;
        }

        var flyout = HomePageContext.ItemContextFlyoutMenu;
        EventHandler<object> flyoutClosed = null!;
        flyoutClosed = (s, e) =>
        {
            flyout!.Closed -= flyoutClosed;

            ListedItem listedItem = new(null!)
            {
                ItemPath = (item!.Item as FileTagsItemViewModel)?.Path ?? string.Empty,
                ItemNameRaw = (item.Item as FileTagsItemViewModel)?.Name ?? string.Empty,
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemType = "Folder".GetLocalizedResource(),
            };
            FilePropertiesHelpers.OpenPropertiesWindow(FolderViewViewModel, listedItem, AppInstance);
        };

        flyout!.Closed += flyoutClosed;
    }

	private void OpenInNewPane(WidgetCardItem? item)
	{
		FileTagsNewPaneInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs()
		{
			Path = item?.Path ?? string.Empty
		});
	}

	private async void FileTagItem_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is FileTagsItemViewModel itemViewModel)
        {
            await itemViewModel.ClickCommand.ExecuteAsync(null);
        }
    }

    private void AdaptiveGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Ensure values are not null
        if (e.OriginalSource is not FrameworkElement element ||
            element.DataContext is not FileTagsItemViewModel item)
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
        var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path), item.IsFolder);
        var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

        // Set max width of the flyout
        secondaryElements
            .OfType<FrameworkElement>()
            .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

        // Add menu items to the secondary flyout
        secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

        // Show the flyout
        itemContextMenuFlyout.ShowAt(element, new() { Position = e.GetPosition(element) });

        // Load shell menu items
        _ = ShellContextmenuHelper.LoadShellMenuItemsAsync(FolderViewViewModel, FlyoutItemPath, itemContextMenuFlyout);

        e.Handled = true;
    }

    public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
	{
		return new List<ContextMenuFlyoutItemViewModel>()
		{
			new()
			{
				Text = "OpenWith".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconOpenWith",
				},
				Tag = "OpenWithPlaceholder",
				ShowItem = !isFolder
			},
			new()
			{
				Text = "SendTo".GetLocalizedResource(),
				Tag = "SendToPlaceholder",
				ShowItem = !isFolder && userSettingsService.GeneralSettingsService.ShowSendToMenu
			},
			new()
			{
				Text = "OpenInNewTab".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconOpenInNewTab",
				},
				Command = OpenInNewTabCommand!,
				CommandParameter = item,
				ShowItem = isFolder
			},
			new()
			{
				Text = "OpenInNewWindow".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconOpenInNewWindow",
				},
				Command = OpenInNewWindowCommand!,
				CommandParameter = item,
				ShowItem = isFolder
			},
			new()
			{
				Text = "OpenFileLocation".GetLocalizedResource(),
				Glyph = "\uED25",
				Command = OpenFileLocationCommand!,
				CommandParameter = item,
				ShowItem = !isFolder
			},
			new()
			{
				Text = "OpenInNewPane".GetLocalizedResource(),
				Command = OpenInNewPaneCommand,
				CommandParameter = item,
				ShowItem = userSettingsService.GeneralSettingsService.ShowOpenInNewPane && isFolder
			},
			new()
			{
				Text = "PinToFavorites".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconPinToFavorites",
				},
				Command = PinToFavoritesCommand!,
				CommandParameter = item,
				ShowItem = !isPinned && isFolder
			},
			new()
			{
				Text = "UnpinFromFavorites".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconUnpinFromFavorites",
				},
				Command = UnpinFromFavoritesCommand!,
				CommandParameter = item,
				ShowItem = isPinned && isFolder
			},
			new()
			{
				Text = "Properties".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconProperties",
				},
				Command = OpenPropertiesCommand!,
				CommandParameter = item,
				ShowItem = isFolder
			},
			new()
			{
				ItemType = ContextMenuFlyoutItemType.Separator,
				Tag = "OverflowSeparator",
			},
			new()
			{
				Text = "Loading".GetLocalizedResource(),
				Glyph = "\xE712",
				Items = [],
				ID = "ItemOverflow",
				Tag = "ItemOverflow",
				IsEnabled = false,
			}
		}.Where(x => x.ShowItem).ToList();
	}

	public void OpenFileLocation(WidgetCardItem? item)
	{
		FileTagsOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
		{
			ItemPath = Directory.GetParent(item?.Path ?? string.Empty)?.FullName ?? string.Empty,
			ItemName = Path.GetFileName(item?.Path ?? string.Empty),
		});
	}

	public Task RefreshWidgetAsync()
	{
		return Task.CompletedTask;
	}

	public void Dispose()
	{
	}
}
