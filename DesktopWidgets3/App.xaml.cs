﻿using System.Diagnostics;
#if !DISABLE_XAML_GENERATED_MAIN
using Microsoft.Extensions.Configuration;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;

namespace DesktopWidgets3;

public partial class App : Application
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(App));

    #region Main Window

    public static MainWindow MainWindow { get; set; } = null!;

#if !DISABLE_XAML_GENERATED_MAIN && SINGLE_INSTANCE
    private static bool IsExistWindow { get; set; } = false;
#endif

#if TRAY_ICON
    public static bool CanCloseWindow { get; set; } = false;
#endif

    #endregion

    #region Tray Icon

#if TRAY_ICON
    public static TrayMenuControl TrayIcon { get; set; } = null!;
#endif

    #endregion

    #region Splash Screen

    public static TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }

    #endregion

    #region Edit Mode Window

    public static EditModeWindow EditModeWindow { get; set; } = null!;

    #endregion

    #region Constructor

    public App()
    {
#if !DISABLE_XAML_GENERATED_MAIN && SINGLE_INSTANCE
        // Check if app is already running
        if (SystemHelper.IsWindowExist(null, ConstantHelper.AppDisplayName, true))
        {
            IsExistWindow = true;
            Current.Exit();
            return;
        }
#endif

        // Initialize the component
        InitializeComponent();

#if !DISABLE_XAML_GENERATED_MAIN
        // Initialize core helpers
        LocalSettingsHelper.Initialize();

        // Set up Logging
        Environment.SetEnvironmentVariable("LOGGING_ROOT", Path.Combine(LocalSettingsHelper.LogDirectory, InfoHelper.GetVersion().ToString()));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
#endif

        // Initialize core helpers
        ResourceExtensions.AddInnerResource(Constants.DevHomeDashboard);

        // Build the host
        var host = Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureLogging(builder => builder
                .AddSerilog(dispose: true))
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateOnBuild = true;
            })
            .ConfigureServices((context, services) =>
            {
                #region Core Service

                // Default Activation Handler
                services.AddSingleton<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                // Other Activation Handlers
                services.AddSingleton<IActivationHandler, AppNotificationActivationHandler>();

                // Windows Activation
                services.AddSingleton<IActivationService, ActivationService>();

                // Notifications
                services.AddSingleton<IAppNotificationService, AppNotificationService>();

                // File Storage
                services.AddSingleton<IFileService, FileService>();

                // Theme Management
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();

                // Backdrop Management
                services.AddSingleton<IBackdropSelectorService, BackdropSelectorService>();

                // Dialog Managment
                services.AddSingleton<IDialogService, DialogService>();

                // Main window: Allow access to the main window
                // from anywhere in the application.
                services.AddSingleton(_ => (Window)MainWindow);

                // DispatcherQueue: Allow access to the DispatcherQueue for
                // the main window for general purpose UI thread access.
                services.AddSingleton(_ => MainWindow.DispatcherQueue);

                #endregion

                #region Navigation Service

                // MainWindow Pages
                services.AddSingleton<IPageService, PageService>();

                // MainWindow Navigation View
                services.AddSingleton<INavigationViewService, NavigationViewService>();

                // MainWindow Navigation
                services.AddSingleton<INavigationService, NavigationService>();

                #endregion

                #region Settings Service

                // Local Storage
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();

                // Settings Management
                services.AddSingleton<IAppSettingsService, AppSettingsService>();

                #endregion

                #region Widget Service

                #region Widget Management

                // Widgets Management
                services.AddSingleton<IWidgetManagerService, WidgetManagerService>();

                // Widgets Resources
                services.AddSingleton<IWidgetResourceService, WidgetResourceService>();

                #endregion

                #region Widget API

                services.AddTransient<ILocalizationService, LocalizationService>();

                services.AddSingleton<ISettingsService, SettingsService>();

                services.AddSingleton<IThemeService, ThemeService>();

                services.AddSingleton<IWidgetService, WidgetService>();

                #endregion

                #endregion

                #region Views & ViewModels

                // Main Window Pages
                services.AddSingleton<NavShellPageViewModel>();
                services.AddTransient<NavShellPage>();
                services.AddSingleton<HomePageViewModel>();
                services.AddTransient<HomePage>();
                services.AddSingleton<SettingsPageViewModel>();
                services.AddTransient<SettingsPage>();
                services.AddSingleton<DashboardPageViewModel>();
                services.AddTransient<DashboardPage>();
                services.AddSingleton<WidgetStorePageViewModel>();
                services.AddTransient<WidgetStorePage>();

                // Widgets Window Pages
                services.AddTransient<WidgetSettingPage>();
                services.AddTransient<WidgetSettingPageViewModel>();
                services.AddTransient<WidgetWindowViewModel>();

                #endregion

                #region Configurations

                // Local Stettings
                services.Configure<LocalSettingsKeys>(context.Configuration.GetSection(nameof(LocalSettingsKeys)));

                #endregion

                #region DevHome

                services.AddSingleton<MicrosoftWidgetModel>();

                services.AddSingleton<IExtensionService, ExtensionService>();

                #endregion

                #region DevHome.Dashboard

                // View-models
                services.AddTransient<AddWidgetViewModel>();

                // DevHome.Dashboard Services
                services.AddDashboard();

                #endregion

                #region DevHome.Services.Core

                // DevHome.Services.Core Services
                services.AddCore();

                #endregion
            })
            .Build();
        DependencyExtensions.ConfigureServices(host.Services);

        // Configure exception handlers
        UnhandledException += (sender, e) => HandleAppUnhandledException(e.Exception, true);
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => HandleAppUnhandledException(e.ExceptionObject as Exception, false);
        TaskScheduler.UnobservedTaskException += (sender, e) => HandleAppUnhandledException(e.Exception, false);

        // Initialize core services
        DependencyExtensions.GetRequiredService<IAppSettingsService>().Initialize();
        DependencyExtensions.GetRequiredService<IAppNotificationService>().Initialize();

        // Initialize core helpers after services
        AppLanguageHelper.Initialize();
        ApplicationExtensionHost.Initialize(this);

        _log.Information($"App initialized. Language: {AppLanguageHelper.PreferredLanguage}.");
    }

    #endregion

    #region App Lifecycle

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

#if !DISABLE_XAML_GENERATED_MAIN && SINGLE_INSTANCE
        if (IsExistWindow)
        {
            return;
        }
#endif

        // Ensure the current window is active
        if (MainWindow != null)
        {
            return;
        }

        _ = ActivateAsync();

        async Task ActivateAsync()
        {
            // Get AppActivationArguments
            var appActivationArguments = AppInstance.GetCurrent().GetActivatedEventArgs();

            // Initialize the window
            MainWindow = new MainWindow();

#if SPLASH_SCREEN
            // Show the splash screen
            SplashScreenLoadingTCS = new TaskCompletionSource();
            var needActivate = await DependencyExtensions.GetRequiredService<IActivationService>().LaunchMainWindowAsync(appActivationArguments);

            if (needActivate)
            {
                // Activate the window
                MainWindow.Activate();
            }
#endif

            _log.Information($"App launched. Launch args type: {args.GetType().Name}.");

#if SPLASH_SCREEN
            static async Task WithTimeoutAsync(Task task, TimeSpan timeout)
            {
                if (task == await Task.WhenAny(task, Task.Delay(timeout)))
                {
                    await task;
                }
            }

            if (needActivate)
            {
                // Wait for the UI to update
                await WithTimeoutAsync(SplashScreenLoadingTCS!.Task, TimeSpan.FromMilliseconds(500));
                SplashScreenLoadingTCS = null;
            }
#endif

            // Initialize dialog service
            DependencyExtensions.GetRequiredService<IDialogService>().Initialize();

            // Check startup
            _ = StartupHelper.CheckStartup();

            // initialize widget store list
            await DependencyExtensions.GetRequiredService<IAppSettingsService>().InitializeWidgetStoreListAsync();

            // Initialize widget resources
            await DependencyExtensions.GetRequiredService<IWidgetResourceService>().InitalizeAsync();

            // initialize widget list
            await DependencyExtensions.GetRequiredService<IAppSettingsService>().InitializeWidgetListAsync();

            // Initialize pinned widgets
            await DependencyExtensions.GetRequiredService<IWidgetManagerService>().InitializePinnedWidgetsAsync(true);

            // Activate the main window
            await DependencyExtensions.GetRequiredService<IActivationService>().ActivateMainWindowAsync(args);

            // Create edit mode window
            EditModeWindow = WindowsExtensions.CreateWindow<EditModeWindow>();
            await DependencyExtensions.GetRequiredService<IActivationService>().ActivateWindowAsync(EditModeWindow);
        }
    }

    private static void HandleAppUnhandledException(Exception? ex, bool showToastNotification)
    {
        var exceptionString = ExceptionFormatter.FormatExcpetion(ex);

        Debug.WriteLine(exceptionString);

        Debugger.Break();

        // Log the error
        _log.Fatal(ex, $"An unhandled error occurred : {exceptionString}");

        // Close the log
        Log.CloseAndFlush();

        // Try to show a notification
        if (showToastNotification)
        {
            DependencyExtensions.GetRequiredService<IAppNotificationService>().TryShow(
                string.Format("AppNotificationUnhandledExceptionPayload".GetLocalizedString(),
                $"{ex?.ToString()}{Environment.NewLine}"));
        }

        // We are very likely in a bad and unrecoverable state, so ensure Dev Home crashes w/ the exception info.
        Environment.FailFast(exceptionString, ex);
    }

#if DISABLE_XAML_GENERATED_MAIN
    public async Task OnActivatedAsync(AppActivationArguments activatedEventArgs)
    {
        _log.Information($"App is activated. Activation type: {activatedEventArgs.Data.GetType().Name}");

        await MainWindow.EnqueueOrInvokeAsync(async (_) => await DependencyExtensions.GetRequiredService<IActivationService>().ActivateMainWindowAsync(activatedEventArgs));
    }
#endif

    public static async new void Exit()
    {
        _log.Information("Exiting current application");

        // Close all desktop widgets 3 widgets
        await DependencyExtensions.GetRequiredService<IWidgetManagerService>().CloseAllWidgetsAsync();

        // Close all windows
        await WindowsExtensions.CloseAllWindowsAsync();

        // Dispose desktop widgets 3 widgets
        await DependencyExtensions.GetRequiredService<IWidgetResourceService>().DisposeWidgetsAsync();

        // Dispose widget manager service
        DependencyExtensions.GetRequiredService<IWidgetManagerService>().Dispose();

        // Dispose microsoft widgets
        DependencyExtensions.GetRequiredService<MicrosoftWidgetModel>().Dispose();

        // Dispose extension service
        DependencyExtensions.GetRequiredService<IExtensionService>().Dispose();

        // Unregister app notification service
        DependencyExtensions.GetRequiredService<IAppNotificationService>().Unregister();

        Current.Exit();
    }

    public static void RestartApplication(string? param = null, bool admin = false)
    {
        _log.Information("Restarting current application with args: {param}, admin: {admin}", param, admin);

        // Get the path to the executable
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;

        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
        {
            // Start a new instance of the application
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                Arguments = param,
                Verb = admin ? "runas" : string.Empty
            });

            // Close the log
            Log.CloseAndFlush();

            // Kill the current process
            Process.GetCurrentProcess().Kill();
        }
    }

    #endregion
}
