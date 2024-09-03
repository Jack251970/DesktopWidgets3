using Files.App.Helpers;
using Files.Core.Services;

using H.NotifyIcon;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using System.Diagnostics;
using System.Text;

using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace DesktopWidgets3;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost? Host { get; private set; }

    public static T GetService<T>() where T : class
    {
        if ((Current as App)!.Host!.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static MainWindow MainWindow { get; set; } = null!;

    private static bool IsExistWindow { get; set; } = false;
    public static bool CanCloseWindow { get; set; } = false;

    public App()
    {
        // Check if app is already running
#if DEBUG
        if (SystemHelper.IsWindowExist(null, "AppDisplayName".GetLocalized(), false))
        {
            // Do nothing here to let the debug app run
        }
#else
        if (SystemHelper.IsWindowExist(null, "AppDisplayName".GetLocalized(), true))
        {
            IsExistWindow = true;
            Current.Exit();
            return;
        }
#endif
        InitializeComponent();

        // The DispatcherQueue event loop exits when all XAML windows on a thread are closed
        // https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.dispatchershutdownmode
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;

        // Initialize core extensions before injecting services
        LocalSettingsExtensions.Initialize();
        LocalSettingsExtensions.RegisterSubFolder("Files");
        ResourceExtensions.AddStringResource("FilesResources");

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureLogging(builder => builder
                .AddProvider(new FileLoggerProvider()))
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

                // Dependency Injection
                services.AddSingleton<IDependencyService, DependencyService>();

                #endregion

                #region Navigation Service

                // MainWindow Shell
                services.AddSingleton<IShellService, ShellService>();

                // MainWindow Pages
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();

                // Widgets Window Pages
                services.AddSingleton<IWidgetPageService, WidgetPageService>();
                services.AddTransient<IWidgetNavigationService, WidgetNavigationService>();

                #endregion

                #region Settings Service

                // Local Storage
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();

                // Settings Management
                services.AddSingleton<DesktopWidgets3.Contracts.Services.IAppSettingsService, DesktopWidgets3.Services.AppSettingsService>();

                #endregion

                #region Functional Service

                // Widget Dialogs
                services.AddSingleton<IWidgetDialogService, WidgetDialogService>();

                // Timers
                services.AddSingleton<ITimersService, TimersService>();

                // Widgets Management
                services.AddSingleton<IWidgetManagerService, WidgetManagerService>();

                // Widgets Resources
                services.AddSingleton<IWidgetResourceService, WidgetResourceService>();

                // System Info
                services.AddSingleton<ISystemInfoService, SystemInfoService>();

                // Window Registeration
                services.AddSingleton<IWindowService, WindowService>();

                #endregion

                #region Views & ViewModels

                // MainwWindow Pages
                services.AddTransient<NavShellPage>();
                services.AddTransient<NavShellViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddTransient<HomePage>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<DashboardPage>();
                services.AddTransient<ClockSettingsViewModel>();
                services.AddTransient<ClockSettingsPage>();
                services.AddTransient<PerformanceSettingsViewModel>();
                services.AddTransient<PerformanceSettingsPage>();
                services.AddTransient<DiskSettingsViewModel>();
                services.AddTransient<DiskSettingsPage>();
                services.AddTransient<FolderViewSettingsViewModel>();
                services.AddTransient<FolderViewSettingsPage>();
                services.AddTransient<NetworkSettingsViewModel>();
                services.AddTransient<NetworkSettingsPage>();

                // Widgets Window Pages
                services.AddTransient<FrameShellPage>();
                services.AddTransient<FrameShellViewModel>();
                services.AddTransient<EditModeOverlayPage>();
                services.AddTransient<EditModeOverlayViewModel>();
                services.AddTransient<ClockViewModel>();
                services.AddTransient<ClockPage>();
                services.AddTransient<PerformanceViewModel>();
                services.AddTransient<PerformancePage>();
                services.AddTransient<DiskViewModel>();
                services.AddTransient<DiskPage>();
                services.AddTransient<FolderViewViewModel>();
                services.AddTransient<FolderViewPage>();
                services.AddTransient<NetworkViewModel>();
                services.AddTransient<NetworkPage>();

                #endregion

                #region Configurations

                // Local Storage
                services.Configure<LocalSettingsKeys>(context.Configuration.GetSection(nameof(LocalSettingsKeys)));
                services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

                #endregion
            })
            .ConfigureHost()
            .Build();

        // Initialize core services
        GetService<IAppNotificationService>().Initialize();

        // Configure exception handlers
        UnhandledException += App_UnhandledException;

        // Initialize core extensions after injecting services
        DependencyExtensions.Initialize(GetService<IDependencyService>());
        LocalSettingsExtensions.RegisterService(GetService<ILocalSettingsService>());
        ThreadExtensions.Initialize(DispatcherQueue.GetForCurrentThread());
        TitleBarHelper.Initialize(GetService<IThemeSelectorService>());
        WindowsExtensions.Initialize(GetService<IWindowService>());
        LogExtensions.Initialize(GetService<ILogger<App>>());
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        HandleAppUnhandledException(e.Exception, true);
        ApplicationLifecycleExtensions.UnhandledException?.Invoke(sender, e);
    }

	public static void HandleAppUnhandledException(Exception? ex, bool showToastNotification)
    {
        StringBuilder formattedException = new()
        {
            Capacity = 200
        };

        formattedException.AppendLine("--------- UNHANDLED EXCEPTION ---------");

        if (ex is not null)
        {
            formattedException.AppendLine($">>>> HRESULT: {ex.HResult}");

            if (ex.Message is not null)
            {
                formattedException.AppendLine("--- MESSAGE ---");
                formattedException.AppendLine(ex.Message);
            }
            if (ex.StackTrace is not null)
            {
                formattedException.AppendLine("--- STACKTRACE ---");
                formattedException.AppendLine(ex.StackTrace);
            }
            if (ex.Source is not null)
            {
                formattedException.AppendLine("--- SOURCE ---");
                formattedException.AppendLine(ex.Source);
            }
            if (ex.InnerException is not null)
            {
                formattedException.AppendLine("--- INNER ---");
                formattedException.AppendLine(ex.InnerException.ToString());
            }
        }
        else
        {
            formattedException.AppendLine("Exception data is not available.");
        }

        formattedException.AppendLine("---------------------------------------");

        Debug.WriteLine(formattedException.ToString());

        Debugger.Break();

        LogExtensions.LogError(ex, ex?.Message ?? "An unhandled error occurred.");

        if (showToastNotification)
        {
            GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), 
                ex?.ToString(), AppContext.BaseDirectory));
        }
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        if (!IsExistWindow && MainWindow is null)
        {
            MainWindow = await WindowsExtensions.GetWindow<MainWindow>(ActivationType.Main, args);
            await GetService<IActivationService>().ActivateMainWindowAsync(args);
            LogExtensions.LogInformation($"App launched. Launch args type: {args.GetType().Name}");
        }
    }

    public static void ShowMainWindow(bool front)
    {
        MainWindow.Show(true);
        if (front)
        {
            MainWindow.Activate();
        }
    }
}
