// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.UI.Controls;
using DesktopWidgets3.Core.Helpers;
using Files.App.UserControls.Sidebar;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;
using Windows.Foundation.Metadata;
using Windows.Services.Store;
using WinRT.Interop;
using VirtualKey = Windows.System.VirtualKey;

namespace Files.App.Views;

public sealed partial class MainPage : Page, INotifyPropertyChanged
{
    // CHANGE: Use tab control model instead tab control component.
    public readonly TabBar TabControl;

    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    public IUserSettingsService UserSettingsService { get; private set; } = null!;
    public IApplicationService ApplicationService { get; }

	public ICommandManager Commands { get; private set; } = null!;

    public IWindowContext WindowContext { get; private set; } = null!;

    public SidebarViewModel SidebarAdaptiveViewModel { get; private set; } = null!;

    public MainPageViewModel ViewModel { get; }

	public StatusCenterViewModel OngoingTasksViewModel { get; private set; } = null!;

    public static AppModel AppModel
		=> App.AppModel;

	/*private bool keyReleased = true;*/

	private bool IsAppRunningAsAdmin => ElevationHelpers.IsAppRunAsAdmin();

	private readonly DispatcherQueueTimer _updateDateDisplayTimer;

	public MainPage()
	{
		InitializeComponent();
        TabControl = new();
        TabControl.Loaded += HorizontalMultitaskingControl_Loaded;

		// Dependency Injection
		/*UserSettingsService = DependencyExtensions.GetService<IUserSettingsService>();*/
		ApplicationService = DependencyExtensions.GetService<IApplicationService>();
        /*Commands = DependencyExtensions.GetService<ICommandManager>();*/
        /*WindowContext = DependencyExtensions.GetService<IWindowContext>();*/
        SidebarAdaptiveViewModel = DependencyExtensions.GetService<SidebarViewModel>();
        SidebarAdaptiveViewModel.PaneFlyout = (MenuFlyout)Resources["SidebarContextMenu"];
        ViewModel = DependencyExtensions.GetService<MainPageViewModel>();
		/*OngoingTasksViewModel = DependencyExtensions.GetService<StatusCenterViewModel>();*/

		if (FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft)
        {
            FlowDirection = FlowDirection.RightToLeft;
        }

		_updateDateDisplayTimer = DispatcherQueue.CreateTimer();
		_updateDateDisplayTimer.Interval = TimeSpan.FromSeconds(1);
		_updateDateDisplayTimer.Tick += UpdateDateDisplayTimer_Tick;
	}

	private async Task PromptForReviewAsync()
	{
		var promptForReviewDialog = new ContentDialog
		{
			Title = "ReviewFiles".GetLocalizedResource(),
			Content = "ReviewFilesContent".GetLocalizedResource(),
			PrimaryButtonText = "Yes".GetLocalizedResource(),
			SecondaryButtonText = "No".GetLocalizedResource()
		};

		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            promptForReviewDialog.XamlRoot = FolderViewViewModel.XamlRoot;
        }

        var result = await promptForReviewDialog.TryShowAsync(FolderViewViewModel);

		if (result == ContentDialogResult.Primary)
		{
			try
			{
				var storeContext = StoreContext.GetDefault();
				InitializeWithWindow.Initialize(storeContext, FolderViewViewModel.WindowHandle);
				var storeRateAndReviewResult = await storeContext.RequestRateAndReviewAppAsync();

				App.Logger?.LogInformation($"STORE: review request status: {storeRateAndReviewResult.Status}");

				UserSettingsService.ApplicationSettingsService.ClickedToReviewApp = true;
			}
			catch (Exception) { }
		}
	}

	private async Task AppRunningAsAdminPromptAsync()
	{
		var runningAsAdminPrompt = new ContentDialog
		{
			Title = "FilesRunningAsAdmin".GetLocalizedResource(),
			Content = "FilesRunningAsAdminContent".GetLocalizedResource(),
			PrimaryButtonText = "Ok".GetLocalizedResource(),
			SecondaryButtonText = "DontShowAgain".GetLocalizedResource()
		};

		var result = await runningAsAdminPrompt.TryShowAsync(FolderViewViewModel);

		if (result == ContentDialogResult.Secondary)
        {
            UserSettingsService.ApplicationSettingsService.ShowRunningAsAdminPrompt = false;
        }
    }

	// WINUI3
	private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
	{
		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            contentDialog.XamlRoot = FolderViewViewModel.XamlRoot;
        }

        return contentDialog;
	}

	private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
	{
		switch (e.SettingName)
		{
			case nameof(IInfoPaneSettingsService.IsEnabled):
				LoadPaneChanged();
				break;
		}
	}

    private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
	{
		/*TabControl.DragArea.SizeChanged += (_, _) => SetRectDragRegion();*/

		if (ViewModel.MultitaskingControl is not TabBar)
		{
			ViewModel.MultitaskingControl = TabControl;
			ViewModel.MultitaskingControls.Add(TabControl);
            ViewModel.MultitaskingControl.CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
        }
    }

    /*private void SetRectDragRegion()
	{
		DragZoneHelper.SetDragZones(
			FolderViewViewModel.MainWindow,
			dragZoneLeftIndent: (int)(TabControl.ActualWidth + TabControl.Margin.Left - TabControl.DragArea.ActualWidth));
	}*/

    public async void TabItemContent_ContentChanged(object? sender, CustomTabViewItemParameter e)
	{
		if (SidebarAdaptiveViewModel.PaneHolder is null)
        {
            return;
        }

        var paneArgs = e.NavigationParameter as PaneNavigationArguments;
		SidebarAdaptiveViewModel.UpdateSidebarSelectedItemFromArgs(SidebarAdaptiveViewModel.PaneHolder.IsLeftPaneActive ?
			paneArgs?.LeftPaneNavPathParam : paneArgs?.RightPaneNavPathParam);

		UpdateStatusBarProperties();
		LoadPaneChanged();
		UpdateNavToolbarProperties();
		await NavigationHelpers.UpdateInstancePropertiesAsync(FolderViewViewModel, paneArgs);
	}

	public void MultitaskingControl_CurrentInstanceChanged(object? sender, CurrentInstanceChangedEventArgs e)
	{
		if (SidebarAdaptiveViewModel.PaneHolder is not null)
        {
            SidebarAdaptiveViewModel.PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;
        }

        var navArgs = e.CurrentInstance.TabItemParameter?.NavigationParameter;
        if (e.CurrentInstance is IPaneHolder currentInstance)
        {
            SidebarAdaptiveViewModel.PaneHolder = currentInstance;
            SidebarAdaptiveViewModel.PaneHolder.PropertyChanged += PaneHolder_PropertyChanged;
		}
		SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged((navArgs as PaneNavigationArguments)?.LeftPaneNavPathParam);

		if (SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.DirectoryPropertiesViewModel is not null)
        {
            SidebarAdaptiveViewModel.PaneHolder.ActivePaneOrColumn.SlimContentPage.DirectoryPropertiesViewModel.ShowLocals = true;
        }

        UpdateStatusBarProperties();
		UpdateNavToolbarProperties();
		LoadPaneChanged();
		_ = NavigationHelpers.UpdateInstancePropertiesAsync(FolderViewViewModel, navArgs);

		e.CurrentInstance.ContentChanged -= TabItemContent_ContentChanged;
		e.CurrentInstance.ContentChanged += TabItemContent_ContentChanged;
	}

	private void PaneHolder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged(SidebarAdaptiveViewModel.PaneHolder.ActivePane?.TabItemParameter?.NavigationParameter?.ToString());
		UpdateStatusBarProperties();
		UpdateNavToolbarProperties();
		LoadPaneChanged();
	}

    private void UpdateStatusBarProperties()
	{
		if (StatusBarControl is not null)
		{
			StatusBarControl.DirectoryPropertiesViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.DirectoryPropertiesViewModel;
			StatusBarControl.SelectedItemsPropertiesViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.SelectedItemsPropertiesViewModel;
		}
	}

    private void UpdateNavToolbarProperties()
	{
		if (NavToolbar is not null)
        {
            NavToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.ToolbarViewModel;
        }

        if (InnerNavigationToolbar is not null)
        {
            InnerNavigationToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.ToolbarViewModel;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
	{
        // CHANGE: Initialize page components, folder view view model and related services.
        if (e.Parameter is IFolderViewViewModel folderViewViewModel)
        {
            TabControl.Initialize(folderViewViewModel);

            FolderViewViewModel = folderViewViewModel;
            FolderViewViewModel.RegisterRightTappedMenu(RightMarginGrid);

            UserSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
            Commands = folderViewViewModel.GetService<ICommandManager>();
            WindowContext = folderViewViewModel.GetService<IWindowContext>();

            SidebarAdaptiveViewModel.Initialize(folderViewViewModel);

            OngoingTasksViewModel = folderViewViewModel.GetService<StatusCenterViewModel>();

            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
        }

        _ = ViewModel.OnNavigatedToAsync(e);

        // CHANGE: Remove sidebar.
        SidebarControl.DisplayMode = SidebarDisplayMode.Minimal;
        SidebarControl.IsPaneOpen = false;
    }

    // CHANGE: Remove keyboard event handlers.
	/*protected async override void OnPreviewKeyDown(KeyRoutedEventArgs e) => await OnPreviewKeyDownAsync(e);

	private async Task OnPreviewKeyDownAsync(KeyRoutedEventArgs e)
	{
		base.OnPreviewKeyDown(e);

		switch (e.Key)
		{
			case VirtualKey.Menu:
			case VirtualKey.Control:
			case VirtualKey.Shift:
			case VirtualKey.LeftWindows:
			case VirtualKey.RightWindows:
				break;
			default:
				var currentModifiers = HotKeyHelpers.GetCurrentKeyModifiers();
				HotKey hotKey = new((Keys)e.Key, currentModifiers);

				// A textbox takes precedence over certain hotkeys.
				if (e.OriginalSource is DependencyObject source && source.FindAscendantOrSelf<TextBox>() is not null)
                {
                    break;
                }

                // Execute command for hotkey
                var command = Commands[hotKey];
				if (command.Code is not CommandCodes.None && keyReleased)
				{
					keyReleased = false;
					e.Handled = command.IsExecutable;
					await command.ExecuteAsync();
				}
				break;
		}
	}

	protected override void OnPreviewKeyUp(KeyRoutedEventArgs e)
	{
		base.OnPreviewKeyUp(e);

		switch (e.Key)
		{
			case VirtualKey.Menu:
			case VirtualKey.Control:
			case VirtualKey.Shift:
			case VirtualKey.LeftWindows:
			case VirtualKey.RightWindows:
				break;
			default:
				keyReleased = true;
				break;
		}
	}

	// A workaround for issue with OnPreviewKeyUp not being called when the hotkey displays a dialog
	protected override void OnLostFocus(RoutedEventArgs e)
	{
		base.OnLostFocus(e);

		keyReleased = true;
	}*/

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		// Defers the status bar loading until after the page has loaded to improve startup perf
		FindName(nameof(StatusBarControl));
		FindName(nameof(InnerNavigationToolbar));
		FindName(nameof(TabControl));
		FindName(nameof(NavToolbar));

		// Notify user that drag and drop is disabled
		// Prompt is disabled in the dev environment to prevent issues with the automation testing 
		// FILESTODO: put this in a StartupPromptService
		if
		(
			ApplicationService.Environment is not AppEnvironment.Dev &&
			IsAppRunningAsAdmin &&
			UserSettingsService.ApplicationSettingsService.ShowRunningAsAdminPrompt
		)
		{
			DispatcherQueue.TryEnqueue(async () => await AppRunningAsAdminPromptAsync());
		}

        // FILESTODO: put this in a StartupPromptService
        if (InfoHelper.GetName() != "49306atecsolution.FilesUWP" || UserSettingsService.ApplicationSettingsService.ClickedToReviewApp)
        {
            return;
        }

        var totalLaunchCount = SystemInformation.Instance.TotalLaunchCount;
		if (totalLaunchCount is 15 or 30 or 60)
		{
			// Prompt user to review app in the Store
			DispatcherQueue.TryEnqueue(async () => await PromptForReviewAsync());
		}
	}

    private void PreviewPane_Loaded(object sender, RoutedEventArgs e)
	{
        // CHANGE: Initalize folder view view model.
        PreviewPane.Initialize(FolderViewViewModel);

        _updateDateDisplayTimer.Start();
	}

	private void PreviewPane_Unloaded(object sender, RoutedEventArgs e)
	{
		_updateDateDisplayTimer.Stop();
	}

	private void UpdateDateDisplayTimer_Tick(object sender, object e)
	{
		if (!App.AppModel.IsMainWindowClosed)
        {
            PreviewPane?.ViewModel!.UpdateDateDisplay();
        }
    }

	private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		switch (PreviewPane?.Position)
		{
			case PreviewPanePositions.Right when ContentColumn.ActualWidth == ContentColumn.MinWidth:
				UserSettingsService.InfoPaneSettingsService.VerticalSizePx += e.NewSize.Width - e.PreviousSize.Width;
				UpdatePositioning();
				break;
			case PreviewPanePositions.Bottom when ContentRow.ActualHeight == ContentRow.MinHeight:
				UserSettingsService.InfoPaneSettingsService.HorizontalSizePx += e.NewSize.Height - e.PreviousSize.Height;
				UpdatePositioning();
				break;
		}
	}

    private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
	{
		// Set the correct tab margin on startup
		SidebarAdaptiveViewModel.UpdateTabControlMargin();
	}

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) => LoadPaneChanged();

	/// <summary>
	/// Call this function to update the positioning of the preview pane.
	/// This is a workaround as the VisualStateManager causes problems.
	/// </summary>
	private void UpdatePositioning()
	{
		if (PreviewPane is null || !ShouldPreviewPaneBeActive)
		{
			PaneRow.MinHeight = 0;
			PaneRow.MaxHeight = double.MaxValue;
			PaneRow.Height = new GridLength(0);
			PaneColumn.MinWidth = 0;
			PaneColumn.MaxWidth = double.MaxValue;
			PaneColumn.Width = new GridLength(0);
		}
		else
		{
			PreviewPane.UpdatePosition(RootGrid.ActualWidth, RootGrid.ActualHeight);
			switch (PreviewPane.Position)
			{
				case PreviewPanePositions.None:
					PaneRow.MinHeight = 0;
					PaneRow.Height = new GridLength(0);
					PaneColumn.MinWidth = 0;
					PaneColumn.Width = new GridLength(0);
					break;
				case PreviewPanePositions.Right:
					PreviewPane.SetValue(Grid.RowProperty, 1);
					PreviewPane.SetValue(Grid.ColumnProperty, 2);
					PaneSplitter.SetValue(Grid.RowProperty, 1);
					PaneSplitter.SetValue(Grid.ColumnProperty, 1);
					PaneSplitter.Width = 2;
					PaneSplitter.Height = RootGrid.ActualHeight;
					PaneSplitter.GripperCursor = GridSplitter.GripperCursorType.SizeWestEast;
					PaneSplitter.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
					PaneColumn.MinWidth = PreviewPane.MinWidth;
					PaneColumn.MaxWidth = PreviewPane.MaxWidth;
					PaneColumn.Width = new GridLength(UserSettingsService.InfoPaneSettingsService.VerticalSizePx, GridUnitType.Pixel);
					PaneRow.MinHeight = 0;
					PaneRow.MaxHeight = double.MaxValue;
					PaneRow.Height = new GridLength(0);
					break;
				case PreviewPanePositions.Bottom:
					PreviewPane.SetValue(Grid.RowProperty, 3);
					PreviewPane.SetValue(Grid.ColumnProperty, 0);
					PaneSplitter.SetValue(Grid.RowProperty, 2);
					PaneSplitter.SetValue(Grid.ColumnProperty, 0);
					PaneSplitter.Height = 2;
					PaneSplitter.Width = RootGrid.ActualWidth;
					PaneSplitter.GripperCursor = GridSplitter.GripperCursorType.SizeNorthSouth;
					PaneSplitter.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth));
					PaneColumn.MinWidth = 0;
					PaneColumn.MaxWidth = double.MaxValue;
					PaneColumn.Width = new GridLength(0);
					PaneRow.MinHeight = PreviewPane.MinHeight;
					PaneRow.MaxHeight = PreviewPane.MaxHeight;
					PaneRow.Height = new GridLength(UserSettingsService.InfoPaneSettingsService.HorizontalSizePx, GridUnitType.Pixel);
					break;
			}
		}
	}

	private void PaneSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
	{
		switch (PreviewPane?.Position)
		{
			case PreviewPanePositions.Right:
				UserSettingsService.InfoPaneSettingsService.VerticalSizePx = PreviewPane.ActualWidth;
				break;
			case PreviewPanePositions.Bottom:
				UserSettingsService.InfoPaneSettingsService.HorizontalSizePx = PreviewPane.ActualHeight;
				break;
		}

		this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
	}

    public bool ShouldViewControlBeDisplayed => SidebarAdaptiveViewModel.PaneHolder?.ActivePane?.InstanceViewModel?.IsPageTypeNotHome ?? false;

    public bool ShouldPreviewPaneBeActive => UserSettingsService.InfoPaneSettingsService.IsEnabled && ShouldPreviewPaneBeDisplayed;

	public bool ShouldPreviewPaneBeDisplayed
	{
		get
		{
			var isHomePage = !(SidebarAdaptiveViewModel.PaneHolder?.ActivePane?.InstanceViewModel?.IsPageTypeNotHome ?? false);
			var isMultiPane = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneActive ?? false;
			var isBigEnough = FolderViewViewModel.MainWindow.Bounds.Width > 450 && FolderViewViewModel.MainWindow.Bounds.Height > 450 || RootGrid.ActualWidth > 700 && FolderViewViewModel.MainWindow.Bounds.Height > 360;
			var isEnabled = (!isHomePage || isMultiPane) && isBigEnough;

			return isEnabled;
		}
	}

	private void LoadPaneChanged()
	{
		OnPropertyChanged(nameof(ShouldViewControlBeDisplayed));
		OnPropertyChanged(nameof(ShouldPreviewPaneBeActive));
		OnPropertyChanged(nameof(ShouldPreviewPaneBeDisplayed));
		UpdatePositioning();
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		if (propertyName == nameof(ShouldPreviewPaneBeActive) && ShouldPreviewPaneBeActive)
        {
            _ = FolderViewViewModel.GetService<InfoPaneViewModel>().UpdateSelectedItemPreviewAsync();
        }
    }

    private void RootGrid_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
	{
		switch (e.Key)
		{
			case VirtualKey.Menu:
			case VirtualKey.Control:
			case VirtualKey.Shift:
			case VirtualKey.LeftWindows:
			case VirtualKey.RightWindows:
				break;
			default:
				var currentModifiers = HotKeyHelpers.GetCurrentKeyModifiers();
				HotKey hotKey = new((Keys)e.Key, currentModifiers);

				// Prevents the arrow key events from navigating the list instead of switching compact overlay
				if (Commands[hotKey].Code is CommandCodes.EnterCompactOverlay or CommandCodes.ExitCompactOverlay)
                {
                    Focus(FocusState.Keyboard);
                }

                break;
		}
	}

    private void NavToolbar_Loaded(object sender, RoutedEventArgs e)
    {
        // CHANGE: Initalize folder view view model.
        NavToolbar.Initialize(FolderViewViewModel);

        UpdateNavToolbarProperties();
    }

    private void InnerNavToolbar_Loaded(object sender, RoutedEventArgs e)
    {
        // CHANGE: Initalize folder view view model.
        InnerNavigationToolbar.Initialize(FolderViewViewModel);

        UpdateNavToolbarProperties();
    }

    private void StatusBar_Loaded(object sender, RoutedEventArgs e)
    {
        // CHANGE: Initalize folder view view model.
        StatusBarControl.Initialize(FolderViewViewModel);
    }

    private void PaneSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
	{
		this.ChangeCursor(InputSystemCursor.Create(PaneSplitter.GripperCursor == GridSplitter.GripperCursorType.SizeWestEast ?
			InputSystemCursorShape.SizeWestEast : InputSystemCursorShape.SizeNorthSouth));
    }

	private void TogglePaneButton_Click(object sender, RoutedEventArgs e)
	{
		if (SidebarControl.DisplayMode == SidebarDisplayMode.Minimal)
		{
			SidebarControl.IsPaneOpen = !SidebarControl.IsPaneOpen;
		}
	}
}
