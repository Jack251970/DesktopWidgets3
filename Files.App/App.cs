// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.Application;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App;

/// <summary>
/// Represents the entry point of UI for Files app.
/// </summary>
public partial class App
{
    public static readonly List<IFolderViewViewModel> FolderViewViewModels = [];

    private readonly IFolderViewViewModel FolderViewViewModel;
    private MainWindow Instance { get; set; } = null!;

    private static bool isInitialized = false;

    // CHANGE: Remove system tray icon.
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
        // CHANGE: Model instead of component.
        /*InitializeComponent();*/

        if (!isInitialized)
        {
            // Configure exception handlers
            ApplicationLifecycleExtensions.UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, true);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.ExceptionObject as Exception, false);
            TaskScheduler.UnobservedTaskException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, false);

            // CHANGE: Configure resouces dispose handler
            ApplicationLifecycleExtensions.MainWindow_Closed_Widgets_Closed += MainWindow_Closed;
        }

        FolderViewViewModel = folderViewViewModel;

        UserSettingsService ??= DependencyExtensions.GetRequiredService<IUserSettingsService>();
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
            // TODO(Later): Add start up argument function.
            var isStartupTask = appActivationArguments.Data is Windows.ApplicationModel.Activation.IStartupTaskActivatedEventArgs;

            if (!isStartupTask)
            {
                // Initialize and activate MainWindow
                MainWindow.Activate();

                // Wait for the Window to initialize
                await Task.Delay(10);

                SplashScreenLoadingTCS = new TaskCompletionSource();
                Instance.ShowSplashScreen();
            }

            // CHANGE: Remove SystemInformation.
            /*// Start tracking app usage
			if (appActivationArguments.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs activationEventArgs)
            {
                SystemInformation.Instance.TrackAppUse(activationEventArgs);
            }*/

            /*// Configure the DI (dependency injection) container
            var host = AppLifecycleHelper.ConfigureHost();
            Ioc.Default.ConfigureServices(host.Services);*/

#if STORE || STABLE || PREVIEW
            if (!isInitialized)
            {
                // Configure Sentry
				AppLifecycleHelper.ConfigureSentry();
            }
#endif

            var userSettingsService = FolderViewViewModel.GetRequiredService<IUserSettingsService>();
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

            if (!isInitialized)
            {
                // FILESTODO: Replace with DI
                QuickAccessManager = DependencyExtensions.GetRequiredService<QuickAccessManager>();
                HistoryWrapper = DependencyExtensions.GetRequiredService<StorageHistoryWrapper>();
                FileTagsManager = DependencyExtensions.GetRequiredService<FileTagsManager>();
                RecentItemsManager = DependencyExtensions.GetRequiredService<RecentItems>();
                LibraryManager = DependencyExtensions.GetRequiredService<LibraryManager>();
                Logger = DependencyExtensions.GetRequiredService<ILogger<App>>();
                AppModel = DependencyExtensions.GetRequiredService<AppModel>();
            }

            // CHANGE: Initialize service instance.
            InitializeServices();

            // CHANGE: Regiter folder view view model and other dictionaries.
            Register(FolderViewViewModel);

            // Hook events for the window
            MainWindow.Closed += Window_Closed;
			MainWindow.Activated += Window_Activated;

			Logger?.LogInformation($"App launched. Launch args type: {appActivationArguments.Data.GetType().Name}");

            if (!(isStartupTask && isLeaveAppRunning))
            {
                // Wait for the UI to update
                await SplashScreenLoadingTCS!.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
                SplashScreenLoadingTCS = null;

                // CHANGE: Remove system tray icon.
                /*// Create a system tray icon
                SystemTrayIcon = new SystemTrayIcon().Show();*/

                _ = Instance.InitializeApplicationAsync(folderPath);
            }
            else
            {
                // CHANGE: Remove system tray icon.
                /*// Create a system tray icon
                SystemTrayIcon = new SystemTrayIcon().Show();*/

                // Sleep current instance
                Program.Pool = new(0, 1, $"Files-{AppLifecycleHelper.AppEnvironment}-Instance");

                Thread.Yield();

                if (Program.Pool.WaitOne())
                {
                    // Resume the instance
                    Program.Pool.Dispose();
                    Program.Pool = null!;
                }
            }

            await AppLifecycleHelper.InitializeAppComponentsAsync(FolderViewViewModel);

            // CHANGE: Set initialized flag.
            if (!isInitialized)
            {
                isInitialized = true;
            }
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
		var userSettingsService = FolderViewViewModel.GetRequiredService<IUserSettingsService>();
		var statusCenterViewModel = FolderViewViewModel.GetRequiredService<StatusCenterViewModel>();

        // A Workaround for the crash (#10110)
        if (_LastOpenedFlyout?.IsOpen ?? false)
		{
			args.Handled = true;
            _LastOpenedFlyout.Closed += async (sender, e) => await WindowsExtensions.CloseWindow(FolderViewViewModel.MainWindow);
            _LastOpenedFlyout.Hide();
			return;
		}

        // Save the current tab list in case it was overwriten by another instance
        AppLifecycleHelper.SaveSessionTabs(FolderViewViewModel);

        if (OutputPath is not null)
        {
            var instance = MainPageViewModel.AppInstancesManager.Get(FolderViewViewModel).FirstOrDefault(x => x.TabItemContent.IsCurrentInstance);
            if (instance is null)
            {
                return;
            }

            var items = (instance.TabItemContent as ShellPanesPage)?.ActivePane?.SlimContentPage?.SelectedItems;
            if (items is null)
            {
                return;
            }

            var results = items.Select(x => x.ItemPath).ToList();
            SystemIO.File.WriteAllLines(OutputPath, results);

            var eventHandle = Win32PInvoke.CreateEvent(IntPtr.Zero, false, false, "FILEDIALOG");
            Win32PInvoke.SetEvent(eventHandle);
            Win32PInvoke.CloseHandle(eventHandle);
        }

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
            FolderViewViewModel.AppWindow.Hide();

            // Close all tabs
            MainPageViewModel.AppInstancesManager.Get(FolderViewViewModel).ForEach(tabItem => tabItem.Unload());
            MainPageViewModel.AppInstancesManager.Get(FolderViewViewModel).Clear();

            // Wait for all properties windows to close
            await FilePropertiesHelpers.WaitClosingAll();

            // Sleep current instance
            Program.Pool = new(0, 1, $"Files-{AppLifecycleHelper.AppEnvironment}-Instance");

            Thread.Yield();

            // Method can take a long time, make sure the window is hidden
            await Task.Yield();

            // Displays a notification the first time the app goes to the background
            if (userSettingsService.AppSettingsService.ShowBackgroundRunningNotification)
            {
                SafetyExtensions.IgnoreExceptions(() =>
                {
                    AppToastNotificationHelper.ShowBackgroundRunningToast();
                });
            }

            if (Program.Pool.WaitOne())
            {
                // Resume the instance
                Program.Pool.Dispose();
                Program.Pool = null!;

                if (!AppModel.ForceProcessTermination)
                {
                    args.Handled = true;
                    // CHANGE: Remove update check.
                    /*_ = AppLifecycleHelper.CheckAppUpdate(FolderViewViewModel);*/
                    return;
                }
            }
        }

        // CHANGE: Unregister folder view view model and other dictionaries.
        Unregister(FolderViewViewModel);

        /*// Method can take a long time, make sure the window is hidden
        await Task.Yield();

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

        // Destroy cached properties windows
        FilePropertiesHelpers.DestroyCachedWindows();
        AppModel.IsMainWindowClosed = true;

        // Wait for ongoing file operations
        FileOperationsHelpers.WaitForCompletion();*/
    }

    // CHANGE: Dispose all resources after main window is closed.
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

    #region register & unregister

    private void Register(IFolderViewViewModel folderViewViewModel)
    {
        if (!FolderViewViewModels.Contains(folderViewViewModel))
        {
            FolderViewViewModels.Add(folderViewViewModel);
        }
        MainPageViewModel.AppInstancesManager.Set(folderViewViewModel, []);
        RecentItemsManager.Register(folderViewViewModel);
    }

    private void Unregister(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModels.Remove(folderViewViewModel);
        MainPageViewModel.AppInstancesManager.Remove(folderViewViewModel);
        RecentItemsManager.Unregister(folderViewViewModel);
    }

    #endregion

    #region Services & Interfaces

    // Settings services
    public IUserSettingsService UserSettingsService { get; private set; }
    public IDevToolsSettingsService DevToolsSettingsService { get; private set; }
    public IActionsSettingsService ActionsSettingsService { get; private set; }
    // Contexts
    public IMultiPanesContext MultiPanesContext { get; private set; }
    public IContentPageContext ContentPageContext { get; private set; }
    public IDisplayPageContext DisplayPageContext { get; private set; }
    public IWindowContext WindowContext { get; private set; }
    public IMultitaskingContext MultitaskingContext { get; private set; }
    // Services
    public IDialogService DialogService { get; private set; }
    public ICommandManager CommandManager { get; private set; }
    public IModifiableCommandManager ModifiableCommandManager { get; private set; }
    public IDateTimeFormatter DateTimeFormatter { get; private set; }
    // ViewModels
    public StatusCenterViewModel StatusCenterViewModel { get; private set; }
    public InfoPaneViewModel InfoPaneViewModel { get; private set; }

    private void InitializeServices()
    {
        // Create services

        // Settings services
        UserSettingsService ??= DependencyExtensions.GetRequiredService<IUserSettingsService>();
        DevToolsSettingsService ??= DependencyExtensions.GetRequiredService<IDevToolsSettingsService>();
        ActionsSettingsService ??= DependencyExtensions.GetRequiredService<IActionsSettingsService>();
        // Contexts
        MultiPanesContext ??= DependencyExtensions.GetRequiredService<IMultiPanesContext>();
        ContentPageContext ??= DependencyExtensions.GetRequiredService<IContentPageContext>();
        DisplayPageContext ??= DependencyExtensions.GetRequiredService<IDisplayPageContext>();
        WindowContext ??= DependencyExtensions.GetRequiredService<IWindowContext>();
        MultitaskingContext ??= DependencyExtensions.GetRequiredService<IMultitaskingContext>();
        // Services
        DialogService ??= DependencyExtensions.GetRequiredService<IDialogService>();
        CommandManager ??= DependencyExtensions.GetRequiredService<ICommandManager>();
        ModifiableCommandManager ??= DependencyExtensions.GetRequiredService<IModifiableCommandManager>();
        DateTimeFormatter ??= DependencyExtensions.GetRequiredService<IDateTimeFormatter>();
        // ViewModels
        StatusCenterViewModel ??= DependencyExtensions.GetRequiredService<StatusCenterViewModel>();
        InfoPaneViewModel ??= DependencyExtensions.GetRequiredService<InfoPaneViewModel>();

        // Initialize services

        // Settings services
        DevToolsSettingsService.Initialize(UserSettingsService);
        ActionsSettingsService.Initialize(UserSettingsService);
        // Contexts
        WindowContext.Initialize(FolderViewViewModel);
        DisplayPageContext.Initialize(FolderViewViewModel);
        MultitaskingContext.Initialize(FolderViewViewModel);
        // Services
        DialogService.Initialize(FolderViewViewModel);
        CommandManager.Initialize(FolderViewViewModel);
        ModifiableCommandManager.Initialize(FolderViewViewModel);
        DateTimeFormatter.Initialize(FolderViewViewModel);
        // ViewModels
        InfoPaneViewModel.Initialize(FolderViewViewModel);
    }

    public T GetRequiredService<T>() where T : class
    {
        return typeof(T) switch
        {
            // Settings services
            Type t when t == typeof(IUserSettingsService) => (UserSettingsService as T)!,
            Type t when t == typeof(IAppearanceSettingsService) => (UserSettingsService.AppearanceSettingsService as T)!,
            Type t when t == typeof(IGeneralSettingsService) => (UserSettingsService.GeneralSettingsService as T)!,
            Type t when t == typeof(IFoldersSettingsService) => (UserSettingsService.FoldersSettingsService as T)!,
            Type t when t == typeof(IDevToolsSettingsService) => (DevToolsSettingsService as T)!,
            Type t when t == typeof(IApplicationSettingsService) => (UserSettingsService.ApplicationSettingsService as T)!,
            Type t when t == typeof(IInfoPaneSettingsService) => (UserSettingsService.InfoPaneSettingsService as T)!,
            Type t when t == typeof(ILayoutSettingsService) => (UserSettingsService.LayoutSettingsService as T)!,
            Type t when t == typeof(IAppSettingsService) => (UserSettingsService.AppSettingsService as T)!,
            Type t when t == typeof(IActionsSettingsService) => (ActionsSettingsService as T)!,
            // Contexts
            Type t when t == typeof(IMultiPanesContext) => (MultiPanesContext as T)!,
            Type t when t == typeof(IContentPageContext) => (ContentPageContext as T)!,
            Type t when t == typeof(IDisplayPageContext) => (DisplayPageContext as T)!,
            Type t when t == typeof(IWindowContext) => (WindowContext as T)!,
            Type t when t == typeof(IMultitaskingContext) => (MultitaskingContext as T)!,
            // Services
            Type t when t == typeof(IDialogService) => (DialogService as T)!,
            Type t when t == typeof(ICommandManager) => (CommandManager as T)!,
            Type t when t == typeof(IModifiableCommandManager) => (ModifiableCommandManager as T)!,
            Type t when t == typeof(IDateTimeFormatter) => (DateTimeFormatter as T)!,
            // ViewModels
            Type t when t == typeof(StatusCenterViewModel) => (StatusCenterViewModel as T)!,
            Type t when t == typeof(InfoPaneViewModel) => (InfoPaneViewModel as T)!,
            _ => null!,
        };
    }

    #endregion
}
