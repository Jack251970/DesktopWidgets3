// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Views;

public sealed partial class HomePage : Page, IDisposable
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

	// Dependency injections

	private IUserSettingsService UserSettingsService { get; set; } = null!;
    private HomeViewModel ViewModel { get; } = DependencyExtensions.GetService<HomeViewModel>();

	// Properties

	private IShellPage AppInstance { get; set; } = null!;

	public LayoutPreferencesManager FolderSettings
		=> AppInstance?.InstanceViewModel.FolderSettings!;

	private QuickAccessWidget? quickAccessWidget;
	private DrivesWidget? drivesWidget;
	private FileTagsWidget? fileTagsWidget;
	private RecentFilesWidget? recentFilesWidget;

	// Constructor

	public HomePage()
	{
		InitializeComponent();

		ViewModel.HomePageLoadedInvoked += ViewModel_HomePageLoadedInvoked;
		ViewModel.WidgetListRefreshRequestedInvoked += ViewModel_WidgetListRefreshRequestedInvoked;
	}

	// Overridden methods

	protected async override void OnNavigatedTo(NavigationEventArgs e)
	{
		if (e.Parameter is not NavigationArguments parameters)
        {
            return;
        }

        FolderViewViewModel = parameters.FolderViewViewModel;

        UserSettingsService = FolderViewViewModel.GetService<IUserSettingsService>();

        AppInstance = parameters.AssociatedTabInstance!;

		AppInstance.InstanceViewModel.IsPageTypeNotHome = false;
		AppInstance.InstanceViewModel.IsPageTypeSearchResults = false;
		AppInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
		AppInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
		AppInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
		AppInstance.InstanceViewModel.IsPageTypeFtp = false;
		AppInstance.InstanceViewModel.IsPageTypeZipFolder = false;
		AppInstance.InstanceViewModel.IsPageTypeLibrary = false;
		AppInstance.InstanceViewModel.GitRepositoryPath = null;
		AppInstance.ToolbarViewModel.CanRefresh = true;
		AppInstance.ToolbarViewModel.CanGoBack = AppInstance.CanNavigateBackward;
		AppInstance.ToolbarViewModel.CanGoForward = AppInstance.CanNavigateForward;
		AppInstance.ToolbarViewModel.CanNavigateToParent = false;

		AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
		AppInstance.ToolbarViewModel.RefreshRequested += ToolbarViewModel_RefreshRequested;

		// Set path of working directory empty
		await AppInstance.FilesystemViewModel.SetWorkingDirectoryAsync("Home");

		AppInstance.SlimContentPage?.DirectoryPropertiesViewModel.UpdateGitInfo(false, string.Empty, null);

		// Clear the path UI and replace with Favorites
		AppInstance.ToolbarViewModel.PathComponents.Clear();

		var componentLabel =
			parameters?.NavPathParam == "Home"
				? "Home".GetLocalizedResource()
				: parameters?.NavPathParam
			?? string.Empty;

		var tag = parameters?.NavPathParam ?? string.Empty;

		var item = new PathBoxItem()
		{
			Title = componentLabel,
			Path = tag,
		};

		AppInstance.ToolbarViewModel.PathComponents.Add(item);

		base.OnNavigatedTo(e);
	}

	protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		base.OnNavigatingFrom(e);

		AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		Dispose();

		base.OnNavigatedFrom(e);
	}

	// Methods

	public void RefreshWidgetList()
	{
		ViewModel.RefreshWidgetList();
	}

	public void ReloadWidgets()
	{
		quickAccessWidget = WidgetsHelpers.TryGetWidget(FolderViewViewModel, UserSettingsService.GeneralSettingsService, ViewModel, out var shouldReloadQuickAccessWidget, quickAccessWidget);
		drivesWidget =      WidgetsHelpers.TryGetWidget(FolderViewViewModel, UserSettingsService.GeneralSettingsService, ViewModel, out var shouldReloadDrivesWidget, drivesWidget);
		fileTagsWidget =    WidgetsHelpers.TryGetWidget(FolderViewViewModel, UserSettingsService.GeneralSettingsService, ViewModel, out var shouldReloadFileTags, fileTagsWidget);
		recentFilesWidget = WidgetsHelpers.TryGetWidget(FolderViewViewModel, UserSettingsService.GeneralSettingsService, ViewModel, out var shouldReloadRecentFiles, recentFilesWidget);

		// Reload QuickAccessWidget
		if (shouldReloadQuickAccessWidget && quickAccessWidget is not null)
		{
			ViewModel.InsertWidget(
				new(
					quickAccessWidget,
					(value) => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded = value,
					() => UserSettingsService.GeneralSettingsService.FoldersWidgetExpanded),
				0);

			quickAccessWidget.CardInvoked -= QuickAccessWidget_CardInvoked;
			quickAccessWidget.CardNewPaneInvoked -= WidgetCardNewPaneInvoked;
			quickAccessWidget.CardPropertiesInvoked -= QuickAccessWidget_CardPropertiesInvoked;
			quickAccessWidget.CardInvoked += QuickAccessWidget_CardInvoked;
			quickAccessWidget.CardNewPaneInvoked += WidgetCardNewPaneInvoked;
			quickAccessWidget.CardPropertiesInvoked += QuickAccessWidget_CardPropertiesInvoked;
		}

		// Reload DrivesWidget
		if (shouldReloadDrivesWidget && drivesWidget is not null)
		{
			ViewModel.InsertWidget(new(drivesWidget, (value) => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.DrivesWidgetExpanded), 1);

			drivesWidget.AppInstance = AppInstance;
			drivesWidget.DrivesWidgetInvoked -= DrivesWidget_DrivesWidgetInvoked;
			drivesWidget.DrivesWidgetNewPaneInvoked -= DrivesWidget_DrivesWidgetNewPaneInvoked;
			drivesWidget.DrivesWidgetInvoked += DrivesWidget_DrivesWidgetInvoked;
			drivesWidget.DrivesWidgetNewPaneInvoked += DrivesWidget_DrivesWidgetNewPaneInvoked;
		}

		// Reload FileTags
		if (shouldReloadFileTags && fileTagsWidget is not null)
		{
			ViewModel.InsertWidget(new(fileTagsWidget, (value) => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.FileTagsWidgetExpanded), 2);

			fileTagsWidget.AppInstance = AppInstance;
			fileTagsWidget.OpenAction = x => NavigationHelpers.OpenPath(FolderViewViewModel, x, AppInstance);
			fileTagsWidget.FileTagsOpenLocationInvoked -= WidgetOpenLocationInvoked;
			fileTagsWidget.FileTagsNewPaneInvoked -= WidgetCardNewPaneInvoked;
			fileTagsWidget.FileTagsOpenLocationInvoked += WidgetOpenLocationInvoked;
			fileTagsWidget.FileTagsNewPaneInvoked += WidgetCardNewPaneInvoked;
			_ = fileTagsWidget.ViewModel.InitAsync(FolderViewViewModel);
		}

		// Reload RecentFilesWidget
		if (shouldReloadRecentFiles && recentFilesWidget is not null)
		{
			ViewModel.InsertWidget(new(recentFilesWidget, (value) => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded = value, () => UserSettingsService.GeneralSettingsService.RecentFilesWidgetExpanded), 4);

			recentFilesWidget.AppInstance = AppInstance;
			recentFilesWidget.RecentFilesOpenLocationInvoked -= WidgetOpenLocationInvoked;
			recentFilesWidget.RecentFileInvoked -= RecentFilesWidget_RecentFileInvoked;
			recentFilesWidget.RecentFilesOpenLocationInvoked += WidgetOpenLocationInvoked;
			recentFilesWidget.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;
		}
	}

	// Event methods

	private void ViewModel_WidgetListRefreshRequestedInvoked(object? sender, EventArgs e)
	{
		ReloadWidgets();
	}

	private void ViewModel_HomePageLoadedInvoked(object? sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		ReloadWidgets();
	}

	private async void RecentFilesWidget_RecentFileInvoked(object sender, PathNavigationEventArgs e)
	{
		try
		{
			if (e.IsFile)
			{
				var directoryName = Path.GetDirectoryName(e.ItemPath);
				await Win32Helpers.InvokeWin32ComponentAsync(e.ItemPath, AppInstance, workingDirectory: directoryName ?? string.Empty);
			}
			else
			{
				AppInstance.NavigateWithArguments(
					FolderSettings.GetLayoutType(e.ItemPath),
					new()
					{
                        FolderViewViewModel = FolderViewViewModel,
						NavPathParam = e.ItemPath
					});
			}
		}
		catch (UnauthorizedAccessException)
		{
			var dialog = DynamicDialogFactory.GetFor_ConsentDialog();

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                dialog.XamlRoot = FolderViewViewModel.XamlRoot;
            }

            await dialog.TryShowAsync(FolderViewViewModel);
		}
		catch (COMException) { }
		catch (ArgumentException) { }
	}

	private void WidgetOpenLocationInvoked(object sender, PathNavigationEventArgs e)
	{
		AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
		{
            FolderViewViewModel = FolderViewViewModel,
			NavPathParam = e.ItemPath,
			SelectItems = new[] { e.ItemName },
			AssociatedTabInstance = AppInstance
		});
	}

	private void QuickAccessWidget_CardInvoked(object sender, QuickAccessCardInvokedEventArgs e)
	{
		AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.Path), new NavigationArguments()
		{
            FolderViewViewModel = FolderViewViewModel,
			NavPathParam = e.Path
		});
	}

	private void WidgetCardNewPaneInvoked(object sender, QuickAccessCardInvokedEventArgs e)
	{
		AppInstance.PaneHolder?.OpenPathInNewPane(e.Path);
	}

	private void QuickAccessWidget_CardPropertiesInvoked(object sender, QuickAccessCardEventArgs e)
	{
		ListedItem listedItem = new(null!)
		{
			ItemPath = e.Item.Path,
			ItemNameRaw = e.Item.Text,
			PrimaryItemAttribute = StorageItemTypes.Folder,
			ItemType = "Folder".GetLocalizedResource(),
		};

		FilePropertiesHelpers.OpenPropertiesWindow(FolderViewViewModel, listedItem, AppInstance);
	}

	private void DrivesWidget_DrivesWidgetNewPaneInvoked(object sender, DrivesWidget.DrivesWidgetInvokedEventArgs e)
	{
		AppInstance.PaneHolder?.OpenPathInNewPane(e.Path);
	}

	private void DrivesWidget_DrivesWidgetInvoked(object sender, DrivesWidget.DrivesWidgetInvokedEventArgs e)
	{
		AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.Path), new NavigationArguments()
		{
            FolderViewViewModel = FolderViewViewModel,
			NavPathParam = e.Path
		});
	}

	private async void ToolbarViewModel_RefreshRequested(object? sender, EventArgs e)
	{
		AppInstance.ToolbarViewModel.CanRefresh = false;
		await Task.WhenAll(ViewModel.WidgetItems.Select(w => w.WidgetItemModel.RefreshWidgetAsync()));
		AppInstance.ToolbarViewModel.CanRefresh = true;
	}

	// Disposer

	public void Dispose()
	{
		ViewModel.HomePageLoadedInvoked -= ViewModel_HomePageLoadedInvoked;
		ViewModel.WidgetListRefreshRequestedInvoked -= ViewModel_WidgetListRefreshRequestedInvoked;
		AppInstance.ToolbarViewModel.RefreshRequested -= ToolbarViewModel_RefreshRequested;
		ViewModel?.Dispose();
	}
}
