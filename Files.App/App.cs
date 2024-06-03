// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Core.Helpers;
using Files.Core.Services.SizeProvider;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App;

/// <summary>
/// Represents the entry point of UI for Files app.
/// </summary>
public partial class App
{
    public static readonly List<IFolderViewViewModel> FolderViewViewModels = new();

    private readonly IFolderViewViewModel FolderViewViewModel;
    private MainWindow Instance { get; set; } = null!;

    private static bool isInitialized;

    /*private static SystemTrayIcon? SystemTrayIcon { get; set; }*/

    public TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }
    public static string? OutputPath { get; set; }

    private CommandBarFlyout? _LastOpenedFlyout;
    public CommandBarFlyout? LastOpenedFlyout
    {
        set
        {
            _LastOpenedFlyout = value;

            if (_LastOpenedFlyout is not null)
            {
                _LastOpenedFlyout.Closed += LastOpenedFlyout_Closed;
            }
        }
    }

    // FILESTODO: Replace with DI
    public static QuickAccessManager QuickAccessManager { get; private set; } = null!;
	public static StorageHistoryWrapper HistoryWrapper { get; private set; } = null!;
	public static FileTagsManager FileTagsManager { get; private set; } = null!;
	public static RecentItems RecentItemsManager { get; private set; } = null!;
	public static LibraryManager LibraryManager { get; private set; } = null!;
	public static AppModel AppModel { get; private set; } = null!;
	public static ILogger Logger { get; private set; } = null!;

	/// <summary>
	/// Initializes an instance of <see cref="App"/>.
	/// </summary>
	public App(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

        // CHANGE: Initialize user settings service, register folder view view model and its app instances.
        UserSettingsService ??= DependencyExtensions.GetService<IUserSettingsService>();
        FolderViewViewModels.Add(folderViewViewModel);
        MainPageViewModel.AppInstances.Add(folderViewViewModel, []);

        /*InitializeComponent();*/

		Initialize();
    }

    private static void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        // Configure exception handlers
        ApplicationLifecycleExtensions.UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, true);
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.ExceptionObject as Exception, false);
        TaskScheduler.UnobservedTaskException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, false);

#if STORE || STABLE || PREVIEW
		// Configure AppCenter
		AppLifecycleHelper.ConfigureAppCenter();
#endif

        // FILESTODO: Replace with DI
        QuickAccessManager = DependencyExtensions.GetService<QuickAccessManager>();
        HistoryWrapper = DependencyExtensions.GetService<StorageHistoryWrapper>();
        FileTagsManager = DependencyExtensions.GetService<FileTagsManager>();
        RecentItemsManager = DependencyExtensions.GetService<RecentItems>();
        LibraryManager = DependencyExtensions.GetService<LibraryManager>();
        Logger = DependencyExtensions.GetService<ILogger<App>>();
        AppModel = DependencyExtensions.GetService<AppModel>();

        // Configure resouces dispose handler
        ApplicationLifecycleExtensions.MainWindow_Closed_Widgets_Closed += MainWindow_Closed;

        // Register theme change handler
        ThemeExtensions.ElementTheme_Changed += (sender, theme) => Helpers.ThemeHelper.RootTheme = theme;

        isInitialized = true;
    }

    /// <summary>
    /// Gets invoked when the application is launched normally by the end user.
    /// </summary>
    public void OnLaunched(string folderPath)
	{
        Instance = new(FolderViewViewModel);

		_ = ActivateAsync();

		async Task ActivateAsync()
		{
            var MainWindow = FolderViewViewModel.MainWindow;

            // Get AppActivationArguments
            var appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            var isStartupTask = appActivationArguments.Data is Windows.ApplicationModel.Activation.IStartupTaskActivatedEventArgs;  // TODO: Add function.

            if (!isStartupTask)
            {
                // Initialize and activate MainWindow
                MainWindow.Activate();

                // Wait for the Window to initialize
                await Task.Delay(10);

                SplashScreenLoadingTCS = new TaskCompletionSource();
                Instance.ShowSplashScreen();
            }

            // CHANGE: Don't track app usage.
			/*// Start tracking app usage
			if (appActivationArguments.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs activationEventArgs)
            {
                SystemInformation.Instance.TrackAppUse(activationEventArgs);
            }*/

            // Configure the DI (dependency injection) container
            InitializeServices();

            var userSettingsService = FolderViewViewModel.GetService<IUserSettingsService>();
            var isLeaveAppRunning = userSettingsService.GeneralSettingsService.LeaveAppRunning;

            if (isStartupTask && !isLeaveAppRunning)
            {
                // Initialize and activate MainWindow
                MainWindow.Activate();

                // Wait for the Window to initialize
                await Task.Delay(10);

                SplashScreenLoadingTCS = new TaskCompletionSource();
                Instance.ShowSplashScreen();
            }

            // Hook events for the window
            MainWindow.Closed += Window_Closed;
			MainWindow.Activated += Window_Activated;

			Logger?.LogInformation($"App launched. Launch args type: {appActivationArguments.Data.GetType().Name}");

            if (!(isStartupTask && isLeaveAppRunning))
            {
                // Wait for the UI to update
                await SplashScreenLoadingTCS!.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
                SplashScreenLoadingTCS = null;

                // CHANGE: Don't create a system tray icon.
                /*// Create a system tray icon
                SystemTrayIcon = new SystemTrayIcon().Show();*/

                _ = Instance.InitializeApplicationAsync(folderPath);
            }
            else
            {
                // CHANGE: Don't create a system tray icon.
                /*// Create a system tray icon
                SystemTrayIcon = new SystemTrayIcon().Show();*/

                // Sleep current instance
                Program.Pool = new(0, 1, $"Files-{ApplicationService.AppEnvironment}-Instance");

                Thread.Yield();

                if (Program.Pool.WaitOne())
                {
                    // Resume the instance
                    Program.Pool.Dispose();
                    Program.Pool = null!;
                }
            }

            await AppLifecycleHelper.InitializeAppComponentsAsync(FolderViewViewModel);
		}
	}

    //// <summary>
    /// Gets invoked when the application is activated.
    /// </summary>
    public async Task OnActivatedAsync(AppActivationArguments activatedEventArgs)
    {
        var activatedEventArgsData = activatedEventArgs.Data;
        Logger.LogInformation($"The app is being activated. Activation type: {activatedEventArgsData.GetType().Name}");

        // InitializeApplication accesses UI, needs to be called on UI thread
        await ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(()
            => Instance.InitializeApplicationAsync(activatedEventArgsData));
    }

    /// <summary>
    /// Gets invoked when the main window is activated.
    /// </summary>
    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        AppModel.IsMainWindowClosed = false;

        // FILESTODO(s): Is this code still needed?
        if (args.WindowActivationState != WindowActivationState.CodeActivated ||
            args.WindowActivationState != WindowActivationState.PointerActivated)
        {
            return;
        }

        LocalSettingsExtensions.SaveLocalSettingAsync("INSTANCE_ACTIVE", -Environment.ProcessId);
    }

    /// <summary>
    /// Gets invoked when the application execution is closed.
    /// </summary>
    /// <remarks>
    /// Saves the current state of the app such as opened tabs, and disposes all cached resources.
    /// </remarks>
    private async void Window_Closed(object sender, WindowEventArgs args)
	{
		// Save application state and stop any background activity
		var userSettingsService = FolderViewViewModel.GetService<IUserSettingsService>();
		var statusCenterViewModel = FolderViewViewModel.GetService<StatusCenterViewModel>();

        // A Workaround for the crash (#10110)
        if (_LastOpenedFlyout?.IsOpen ?? false)
		{
			args.Handled = true;
            _LastOpenedFlyout.Closed += async (sender, e) => await WindowsExtensions.CloseWindow(FolderViewViewModel.MainWindow);
            _LastOpenedFlyout.Hide();
			return;
		}

        AppLifecycleHelper.SaveSessionTabs(FolderViewViewModel);

        // Continue running the app on the background
        if (userSettingsService.GeneralSettingsService.LeaveAppRunning &&
			!AppModel.ForceProcessTermination &&
            !Process.GetProcessesByName("Files").Any(x => x.Id != Environment.ProcessId))
        {
            // Close open content dialogs
            UIHelpers.CloseAllDialogs(FolderViewViewModel);

            // Close all notification banners except in progress
            statusCenterViewModel.RemoveAllCompletedItems();

            // Cache the window instead of closing it
            FolderViewViewModel.MainWindow.AppWindow.Hide();

            // Close all tabs
            MainPageViewModel.AppInstances[FolderViewViewModel].ForEach(tabItem => tabItem.Unload());
            MainPageViewModel.AppInstances[FolderViewViewModel].Clear();

            // Wait for all properties windows to close
            await FilePropertiesHelpers.WaitClosingAll();

            // Sleep current instance
            Program.Pool = new(0, 1, $"Files-{ApplicationService.AppEnvironment}-Instance");

            Thread.Yield();

            if (Program.Pool.WaitOne())
            {
                // Resume the instance
                Program.Pool.Dispose();
                Program.Pool = null!;

                if (!AppModel.ForceProcessTermination)
                {
                    args.Handled = true;
                    // CHANGE: Don't check app updates.
                    /*_ = AppLifecycleHelper.CheckAppUpdate(FolderViewViewModel);*/
                    return;
                }
            }
        }

        // Method can take a long time, make sure the window is hidden
        await Task.Yield();

        if (OutputPath is not null)
		{
			await SafetyExtensions.IgnoreExceptions(async () =>
			{
				var instance = MainPageViewModel.AppInstances[FolderViewViewModel].FirstOrDefault(x => x.TabItemContent.IsCurrentInstance);
				if (instance is null)
                {
                    return;
                }

                var items = (instance.TabItemContent as PaneHolderPage)?.ActivePane?.SlimContentPage?.SelectedItems;
				if (items is null)
                {
                    return;
                }

                await FileIO.WriteLinesAsync(await StorageFile.GetFileFromPathAsync(OutputPath), items.Select(x => x.ItemPath));
			},
			Logger);
		}

        // CHANGE: Register folder view view model and its app instances.
        MainPageViewModel.AppInstances.Remove(FolderViewViewModel);
        FolderViewViewModels.Remove(FolderViewViewModel);
    }

    private static void MainWindow_Closed(object sender, WindowEventArgs e)
    {
        // Try to maintain clipboard data after app close
        SafetyExtensions.IgnoreExceptions(() =>
        {
            var dataPackage = Clipboard.GetContent();
            if (dataPackage.Properties.PackageFamilyName == InfoHelper.GetFamilyName())
            {
                if (dataPackage.Contains(StandardDataFormats.StorageItems))
                {
                    Clipboard.Flush();
                }
            }
        },
        Logger);

        // Dispose git operations' thread
        GitHelpers.TryDispose();

        // Destroy cached properties windows
        FilePropertiesHelpers.DestroyCachedWindows();
        AppModel.IsMainWindowClosed = true;

        // Wait for ongoing file operations
        FileOperationsHelpers.WaitForCompletion();
    }

    /// <summary>
    /// Gets invoked when the last opened flyout is closed.
    /// </summary>
    private void LastOpenedFlyout_Closed(object? sender, object e)
    {
        if (sender is not CommandBarFlyout commandBarFlyout)
        {
            return;
        }

        commandBarFlyout.Closed -= LastOpenedFlyout_Closed;
        if (_LastOpenedFlyout == commandBarFlyout)
        {
            _LastOpenedFlyout = null;
        }
    }

    #region Services & Interfaces

    public ICommandManager CommandManager { get; private set; } = null!;
    public IModifiableCommandManager ModifiableCommandManager { get; private set; } = null!;
    public IDialogService DialogService { get; private set; } = null!;
    public StatusCenterViewModel StatusCenterViewModel { get; private set; } = null!;
    public InfoPaneViewModel InfoPaneViewModel { get; private set; } = null!;
    public IUserSettingsService UserSettingsService { get; private set; } = null!;
    public IDateTimeFormatter DateTimeFormatter { get; private set; } = null!;
    public ISizeProvider SizeProvider { get; private set; } = null!;

    public IWindowContext WindowContext { get; private set; } = null!;
    public IContentPageContext ContentPageContext { get; private set; } = null!;
    public IPageContext PageContext { get; private set; } = null!;
    public IDisplayPageContext DisplayPageContext { get; private set; } = null!;
    public IMultitaskingContext MultitaskingContext { get; private set; } = null!;

    private void InitializeServices()
    {
        CommandManager ??= DependencyExtensions.GetService<ICommandManager>();
        ModifiableCommandManager ??= DependencyExtensions.GetService<IModifiableCommandManager>();
        DialogService ??= DependencyExtensions.GetService<IDialogService>();
        StatusCenterViewModel ??= DependencyExtensions.GetService<StatusCenterViewModel>();
        InfoPaneViewModel ??= DependencyExtensions.GetService<InfoPaneViewModel>();
        UserSettingsService ??= DependencyExtensions.GetService<IUserSettingsService>();
        DateTimeFormatter ??= DependencyExtensions.GetService<IDateTimeFormatter>();
        SizeProvider ??= DependencyExtensions.GetService<ISizeProvider>();

        WindowContext ??= DependencyExtensions.GetService<IWindowContext>();
        ContentPageContext ??= DependencyExtensions.GetService<IContentPageContext>();
        PageContext ??= DependencyExtensions.GetService<IPageContext>();
        DisplayPageContext ??= DependencyExtensions.GetService<IDisplayPageContext>();
        MultitaskingContext ??= DependencyExtensions.GetService<IMultitaskingContext>();

        WindowContext.Initialize(FolderViewViewModel);
        DisplayPageContext.Initialize(FolderViewViewModel);
        MultitaskingContext.Initialize(FolderViewViewModel);

        CommandManager.Initialize(FolderViewViewModel);
        ModifiableCommandManager.Initialize(CommandManager);
        DialogService.Initialize(FolderViewViewModel);
        InfoPaneViewModel.Initialize(FolderViewViewModel);
        DateTimeFormatter.Initialize(FolderViewViewModel);
    }

    public T GetService<T>() where T : class
    {
        return typeof(T) switch
        {
            Type t when t == typeof(ICommandManager) => (CommandManager as T)!,
            Type t when t == typeof(IModifiableCommandManager) => (ModifiableCommandManager as T)!,
            Type t when t == typeof(IDialogService) => (DialogService as T)!,
            Type t when t == typeof(StatusCenterViewModel) => (StatusCenterViewModel as T)!,
            Type t when t == typeof(InfoPaneViewModel) => (InfoPaneViewModel as T)!,
            Type t when t == typeof(IUserSettingsService) => (UserSettingsService as T)!,
            Type t when t == typeof(IGeneralSettingsService) => (UserSettingsService.GeneralSettingsService as T)!,
            Type t when t == typeof(IFoldersSettingsService) => (UserSettingsService.FoldersSettingsService as T)!,
            Type t when t == typeof(IAppearanceSettingsService) => (UserSettingsService.AppearanceSettingsService as T)!,
            Type t when t == typeof(IApplicationSettingsService) => (UserSettingsService.ApplicationSettingsService as T)!,
            Type t when t == typeof(IInfoPaneSettingsService) => (UserSettingsService.InfoPaneSettingsService as T)!,
            Type t when t == typeof(ILayoutSettingsService) => (UserSettingsService.LayoutSettingsService as T)!,
            Type t when t == typeof(IDateTimeFormatter) => (DateTimeFormatter as T)!,
            Type t when t == typeof(ISizeProvider) => (SizeProvider as T)!,
            Type t when t == typeof(IAppSettingsService) => (UserSettingsService.AppSettingsService as T)!,
            Type t when t == typeof(IWindowContext) => (WindowContext as T)!,
            Type t when t == typeof(IContentPageContext) => (ContentPageContext as T)!,
            Type t when t == typeof(IPageContext) => (PageContext as T)!,
            Type t when t == typeof(IDisplayPageContext) => (DisplayPageContext as T)!,
            Type t when t == typeof(IMultitaskingContext) => (MultitaskingContext as T)!,
            _ => null!,
        };
    }

    #endregion
}
