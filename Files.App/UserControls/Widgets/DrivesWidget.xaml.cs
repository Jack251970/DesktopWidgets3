// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Widgets;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class DriveCardItem : WidgetCardItem, IWidgetCardItem<DriveItem>, IComparable<DriveCardItem>
{
	private BitmapImage thumbnail;
	private byte[] thumbnailData;

	public new DriveItem Item { get; private set; }
	public bool HasThumbnail => thumbnail is not null && thumbnailData is not null;
	public BitmapImage Thumbnail
	{
		get => thumbnail;
		set => SetProperty(ref thumbnail, value);
	}
	public DriveCardItem(DriveItem item)
	{
		Item = item;
		Path = item.Path;
	}

	public async Task LoadCardThumbnailAsync()
	{
		// Try load thumbnail using ListView mode
		if (thumbnailData is null || thumbnailData.Length == 0)
        {
            thumbnailData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.Path, Convert.ToUInt32(Constants.Widgets.WidgetIconSize), Windows.Storage.FileProperties.ThumbnailMode.SingleItem, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
        }

        // Thumbnail is still null, use DriveItem icon (loaded using SingleItem mode)
        if (thumbnailData is null || thumbnailData.Length == 0)
		{
			await Item.LoadThumbnailAsync();
			thumbnailData = Item.IconData;
		}

		// Thumbnail data is valid, set the item icon
		if (thumbnailData is not null && thumbnailData.Length > 0)
        {
            Thumbnail = (await UIThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(() => thumbnailData.ToBitmapAsync(Constants.Widgets.WidgetIconSize)))!;
        }
    }

	public int CompareTo(DriveCardItem? other) => Item.Path.CompareTo(other?.Item?.Path);
}

public sealed partial class DrivesWidget : HomePageWidget, IWidgetItem, INotifyPropertyChanged
{
    public IUserSettingsService userSettingsService;

	private readonly DrivesViewModel drivesViewModel = DependencyExtensions.GetService<DrivesViewModel>();

	private readonly NetworkDrivesViewModel networkDrivesViewModel = DependencyExtensions.GetService<NetworkDrivesViewModel>();

	public delegate void DrivesWidgetInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

	public event DrivesWidgetInvokedEventHandler DrivesWidgetInvoked;

	public delegate void DrivesWidgetNewPaneInvokedEventHandler(object sender, DrivesWidgetInvokedEventArgs e);

	public event DrivesWidgetNewPaneInvokedEventHandler DrivesWidgetNewPaneInvoked;

	public event PropertyChangedEventHandler? PropertyChanged;

	public static ObservableCollection<DriveCardItem> ItemsAdded = new();

	private IShellPage associatedInstance;

	public ICommand FormatDriveCommand;
	public ICommand EjectDeviceCommand;
	public ICommand DisconnectNetworkDriveCommand;
	public ICommand GoToStorageSenseCommand;
	public ICommand OpenInNewPaneCommand;

	public IShellPage AppInstance
	{
		get => associatedInstance;
		set
		{
			if (value != associatedInstance)
			{
				associatedInstance = value;
				NotifyPropertyChanged(nameof(AppInstance));
			}
		}
	}

	public string WidgetName => nameof(DrivesWidget);

	public string AutomationProperties => "DrivesWidgetAutomationProperties/Name".GetLocalizedResource();

	public string WidgetHeader => "Drives".GetLocalizedResource();

	public bool IsWidgetSettingEnabled => base.UserSettingsService.GeneralSettingsService.ShowDrivesWidget;

	public bool ShowMenuFlyout => true;

	public MenuFlyoutItem MenuFlyoutItem => new()
	{
		Icon = new FontIcon() { Glyph = "\uE710" },
		Text = "DrivesWidgetOptionsFlyoutMapNetDriveMenuItem/Text".GetLocalizedResource(),
		Command = MapNetworkDriveCommand
	};

	public AsyncRelayCommand MapNetworkDriveCommand { get; }

	public DrivesWidget()
	{
		InitializeComponent();

		Drives_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

		drivesViewModel.Drives.CollectionChanged += Drives_CollectionChanged;

		FormatDriveCommand = new RelayCommand<DriveCardItem>(FormatDrive);
		EjectDeviceCommand = new AsyncRelayCommand<DriveCardItem>(EjectDeviceAsync);
		OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewTabAsync);
		OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(OpenInNewWindowAsync);
		OpenInNewPaneCommand = new AsyncRelayCommand<DriveCardItem>(OpenInNewPaneAsync);
		OpenPropertiesCommand = new RelayCommand<DriveCardItem>(OpenProperties);
		PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(PinToFavoritesAsync);
		UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(UnpinFromFavoritesAsync);
		MapNetworkDriveCommand = new AsyncRelayCommand(DoNetworkMapDriveAsync); 
		DisconnectNetworkDriveCommand = new RelayCommand<DriveCardItem>(DisconnectNetworkDrive);
	}

    public new void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        base.Initialize(folderViewViewModel);

        userSettingsService = UserSettingsService;
    }

	private async void Drives_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		await DispatcherQueue.EnqueueOrInvokeAsync(async () =>
		{
			foreach (var drive in drivesViewModel.Drives.ToList().Cast<DriveItem>())
			{
				if (!ItemsAdded.Any(x => x.Item == drive) && drive.Type != DriveType.VirtualDrive)
				{
					var cardItem = new DriveCardItem(drive);
					ItemsAdded.AddSorted(cardItem);
					await cardItem.LoadCardThumbnailAsync(); // After add
				}
			}

			foreach (var driveCard in ItemsAdded.ToList())
			{
				if (!drivesViewModel.Drives.Contains(driveCard.Item))
                {
                    ItemsAdded.Remove(driveCard);
                }
            }
		});
	}

	public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
	{
		var drive = ItemsAdded.Where(x => string.Equals(PathNormalization.NormalizePath(x.Path), PathNormalization.NormalizePath(item.Path), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
		var options = drive?.Item.MenuOptions;

		return new List<ContextMenuFlyoutItemViewModel>()
		{
			new()
			{
				Text = "OpenInNewTab".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconOpenInNewTab",
				},
				Command = OpenInNewTabCommand,
				CommandParameter = item,
				ShowItem = userSettingsService.GeneralSettingsService.ShowOpenInNewTab
			},
			new()
			{
				Text = "OpenInNewWindow".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconOpenInNewWindow",
				},
				Command = OpenInNewWindowCommand,
				CommandParameter = item,
				ShowItem = userSettingsService.GeneralSettingsService.ShowOpenInNewWindow
			},
			new()
			{
				Text = "OpenInNewPane".GetLocalizedResource(),
				Command = OpenInNewPaneCommand,
				CommandParameter = item,
				ShowItem = userSettingsService.GeneralSettingsService.ShowOpenInNewPane
			},
			new()
			{
				Text = "PinToFavorites".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconPinToFavorites",
				},
				Command = PinToFavoritesCommand,
				CommandParameter = item,
				ShowItem = !isPinned
			},
			new()
			{
				Text = "UnpinFromFavorites".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconUnpinFromFavorites",
				},
				Command = UnpinFromFavoritesCommand,
				CommandParameter = item,
				ShowItem = isPinned
			},
			new()
			{
				Text = "Eject".GetLocalizedResource(),
				Command = EjectDeviceCommand,
				CommandParameter = item,
				ShowItem = options?.ShowEjectDevice ?? false
			},
			new()
			{
				Text = "FormatDriveText".GetLocalizedResource(),
				Command = FormatDriveCommand,
				CommandParameter = item,
				ShowItem = options?.ShowFormatDrive ?? false
			},
			new()
			{
				Text = "Properties".GetLocalizedResource(),
				OpacityIcon = new OpacityIconModel()
				{
					OpacityIconStyle = "ColorIconProperties",
				},
				Command = OpenPropertiesCommand,
				CommandParameter = item
			},
			new()
			{
				Text = "TurnOnBitLocker".GetLocalizedResource(),
				Tag = "TurnOnBitLockerPlaceholder",
				IsEnabled = false
			},
			new()
			{
				Text = "ManageBitLocker".GetLocalizedResource(),
				Tag = "ManageBitLockerPlaceholder",
				IsEnabled = false
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
				Items = new List<ContextMenuFlyoutItemViewModel>(),
				ID = "ItemOverflow",
				Tag = "ItemOverflow",
				IsEnabled = false,
			}
		}.Where(x => x.ShowItem).ToList();
	}

	private Task DoNetworkMapDriveAsync()
	{
		return networkDrivesViewModel.OpenMapNetworkDriveDialogAsync(FolderViewViewModel);
	}

	private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private async Task EjectDeviceAsync(DriveCardItem? item)
	{
		var result = await DriveHelpers.EjectDeviceAsync(item!.Item.Path);
		await UIHelpers.ShowDeviceEjectResultAsync(FolderViewViewModel, item.Item.Type, result);
	}

	private void FormatDrive(DriveCardItem? item)
	{
		Win32API.OpenFormatDriveDialog(item?.Path ?? string.Empty);
	}

	private void OpenProperties(DriveCardItem? item)
	{
		EventHandler<object> flyoutClosed = null!;
		flyoutClosed = async (s, e) =>
		{
			ItemContextMenuFlyout.Closed -= flyoutClosed;
			FilePropertiesHelpers.OpenPropertiesWindow(FolderViewViewModel, item!.Item, associatedInstance);
            await Task.CompletedTask;
		};
		ItemContextMenuFlyout.Closed += flyoutClosed;
	}

	private async void Button_Click(object sender, RoutedEventArgs e)
	{
		var ClickedCard = ((Button)sender).Tag.ToString();
		var NavigationPath = ClickedCard; // path to navigate

		if (await DriveHelpers.CheckEmptyDrive(FolderViewViewModel, NavigationPath))
        {
            return;
        }

        var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
		if (ctrlPressed)
		{
			await NavigationHelpers.OpenPathInNewTab(FolderViewViewModel, NavigationPath);
			return;
		}

		DrivesWidgetInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
		{
			Path = NavigationPath!
		});
	}

	private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed) // check middle click
        {
            return;
        }

        var navigationPath = (sender as Button)!.Tag.ToString();
		if (await DriveHelpers.CheckEmptyDrive(FolderViewViewModel, navigationPath))
        {
            return;
        }

        await NavigationHelpers.OpenPathInNewTab(FolderViewViewModel, navigationPath);
	}

	public class DrivesWidgetInvokedEventArgs : EventArgs
	{
		public string Path { get; set; }
	}

	private async Task OpenInNewPaneAsync(DriveCardItem? item)
	{
		if (await DriveHelpers.CheckEmptyDrive(FolderViewViewModel, item!.Item.Path))
        {
            return;
        }

        DrivesWidgetNewPaneInvoked?.Invoke(this, new DrivesWidgetInvokedEventArgs()
		{
			Path = item.Item.Path
		});
	}

	private void MenuFlyout_Opening(object sender, object e)
	{
		var pinToFavoritesItem = (sender as MenuFlyout)!.Items.Single(x => x.Name == "PinToFavorites");
		pinToFavoritesItem.Visibility = (pinToFavoritesItem.DataContext as DriveItem)!.IsPinned ? Visibility.Collapsed : Visibility.Visible;

		var unpinFromFavoritesItem = (sender as MenuFlyout)!.Items.Single(x => x.Name == "UnpinFromFavorites");
		unpinFromFavoritesItem.Visibility = (unpinFromFavoritesItem.DataContext as DriveItem)!.IsPinned ? Visibility.Visible : Visibility.Collapsed;
	}

	private void DisconnectNetworkDrive(DriveCardItem? item)
	{
		networkDrivesViewModel.DisconnectNetworkDrive(item!.Item);
	}

	private void GoToStorageSense_Click(object sender, RoutedEventArgs e)
	{
		var clickedCard = (sender as Button)!.Tag.ToString();
        _ = StorageSenseHelper.OpenStorageSenseAsync(clickedCard!);
	}

	public async Task RefreshWidgetAsync()
	{
		var updateTasks = ItemsAdded.Select(item => item.Item.UpdatePropertiesAsync());
		await Task.WhenAll(updateTasks);
	}

	public void Dispose()
	{

	}
}
