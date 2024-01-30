// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel;
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

    public TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }
	public CommandBarFlyout? LastOpenedFlyout { get; set; }
    public static string? OutputPath { get; set; }

    // FILESTODO: Replace with DI
    public static QuickAccessManager QuickAccessManager { get; private set; } = null!;
	public static StorageHistoryWrapper HistoryWrapper { get; private set; } = null!;
	public static FileTagsManager FileTagsManager { get; private set; } = null!;
	/*public static RecentItems RecentItemsManager { get; private set; } = null!;*/
	public static LibraryManager LibraryManager { get; private set; } = null!;
	public static AppModel AppModel { get; private set; } = null!;
	public static ILogger Logger { get; private set; } = null!;

	/// <summary>
	/// Initializes an instance of <see cref="App"/>.
	/// </summary>
	public App(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

        // CHANGE: Register folder view view model and its app instances.
        FolderViewViewModels.Add(folderViewViewModel);
        MainPageViewModel.AppInstances.Add(folderViewViewModel, new());

        /*InitializeComponent();*/

		// Configure exception handlers and resouces dispose handlers
		Initialize();
    }

    private static void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        // Configure exception handlers
        ApplicationExtensions.UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, true);
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.ExceptionObject as Exception, false);
        TaskScheduler.UnobservedTaskException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, false);
        
        ApplicationExtensions.MainWindow_Closed_Widgets_Closed += MainWindow_Closed;

        isInitialized = true;
    }

	/// <summary>
	/// Invoked when the application is launched normally by the end user.
	/// Other entry points will be used such as when the application is launched to open a specific file.
	/// </summary>
	public void OnLaunched(string folderPath)
	{
        Instance = new(FolderViewViewModel);

		_ = ActivateAsync();

		async Task ActivateAsync()
		{
            var MainWindow = FolderViewViewModel.MainWindow;

            // Initialize and activate MainWindow
            MainWindow.Activate();

			// Wait for the Window to initialize
			await Task.Delay(10);

			SplashScreenLoadingTCS = new TaskCompletionSource();
			Instance.ShowSplashScreen();

            /*// Get AppActivationArguments
			var appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

			// Start tracking app usage
			if (appActivationArguments.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs activationEventArgs)
            {
                SystemInformation.Instance.TrackAppUse(activationEventArgs);
            }

            // Configure the DI (dependency injection) container
            var host = AppLifecycleHelper.ConfigureHost();
			Ioc.Default.ConfigureServices(host.Services);*/

#if STORE || STABLE || PREVIEW
			// Configure AppCenter
			AppLifecycleHelper.ConfigureAppCenter();
#endif

            // FILESTODO: Replace with DI
            QuickAccessManager = DependencyExtensions.GetService<QuickAccessManager>();
			HistoryWrapper = DependencyExtensions.GetService<StorageHistoryWrapper>();
			FileTagsManager = DependencyExtensions.GetService<FileTagsManager>();
            /*RecentItemsManager = DependencyExtensions.GetService<RecentItems>();*/
            LibraryManager = DependencyExtensions.GetService<LibraryManager>();
			Logger = DependencyExtensions.GetService<ILogger<App>>();
			AppModel = DependencyExtensions.GetService<AppModel>();

			// Hook events for the window
			MainWindow.Closed += Window_Closed;
			/*MainWindow.Activated += Window_Activated;*/

			/*Logger?.LogInformation($"App launched. Launch args type: {appActivationArguments.Data.GetType().Name}");*/

			// Wait for the UI to update
			await SplashScreenLoadingTCS!.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
			SplashScreenLoadingTCS = null;

			_ = AppLifecycleHelper.InitializeAppComponentsAsync(FolderViewViewModel);
			_ = Instance.InitializeApplicationAsync(folderPath);
		}
	}

    /*/// <summary>
	/// Invoked when the application is activated.
	/// </summary>
	public static void OnActivated(AppActivationArguments activatedEventArgs)
	{
		Logger.LogInformation($"The app is being activated. Activation type: {activatedEventArgs.Data.GetType().Name}");

		// InitializeApplication accesses UI, needs to be called on UI thread
		_ = UIThreadExtensions.DispatcherQueue.EnqueueOrInvokeAsync(()
			=> MainWindow.Instance.InitializeApplicationAsync(activatedEventArgs.Data));
	}

	/// <summary>
	/// Invoked when the main window is activated.
	/// </summary>
	private void Window_Activated(object sender, WindowActivatedEventArgs args)
	{
		// FILESTODO(s): Is this code still needed?
		if (args.WindowActivationState != WindowActivationState.CodeActivated ||
			args.WindowActivationState != WindowActivationState.PointerActivated)
        {
            return;
        }

        ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Environment.ProcessId;
	}*/

    /// <summary>
    /// Invoked when application execution is being closed. Save application state.
    /// </summary>
    private async void Window_Closed(object sender, WindowEventArgs args)
	{
		// Save application state and stop any background activity
		var userSettingsService = FolderViewViewModel.GetService<IUserSettingsService>();
		var statusCenterViewModel = FolderViewViewModel.GetService<StatusCenterViewModel>();

        // A Workaround for the crash (#10110)
        /*if (LastOpenedFlyout?.IsOpen ?? false)
		{
			args.Handled = true;
			LastOpenedFlyout.Closed += (sender, e) => App.Current.Exit();
			LastOpenedFlyout.Hide();
			return;
		}*/

        /*if (userSettingsService.GeneralSettingsService.LeaveAppRunning &&
			!AppModel.ForceProcessTermination &&
			!Process.GetProcessesByName("Files").Any(x => x.Id != Environment.ProcessId))
		{
            // Close open content dialogs
            UIHelpers.CloseAllDialogs(FolderViewViewModel);

            // Close all notification banners except in progress
            statusCenterViewModel.RemoveAllCompletedItems();

            // Cache the window instead of closing it
            FolderViewViewModel.MainWindow.AppWindow.Hide();
            args.Handled = true;

            // Save and close all tabs
            AppLifecycleHelper.SaveSessionTabs(FolderViewViewModel);
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

                _ = AppLifecycleHelper.CheckAppUpdate();
            }

            return;
        }*/

        // Method can take a long time, make sure the window is hidden
        await Task.Yield();

		AppLifecycleHelper.SaveSessionTabs(FolderViewViewModel);

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

		/*// Try to maintain clipboard data after app close
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
		FileOperationsHelpers.WaitForCompletion();*/
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
}
