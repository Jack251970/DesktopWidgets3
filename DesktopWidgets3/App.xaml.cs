using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace DesktopWidgets3;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    private static string ClassName => typeof(App).Name;

    #region Host

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

    #endregion

    #region Main Window

    public static MainWindow MainWindow { get; set; } = null!;

    private static bool IsExistWindow { get; set; } = false;
    public static bool CanCloseWindow { get; set; } = false;

    public static void ShowMainWindow(bool front)
    {
        MainWindow.Show();
        if (front)
        {
            MainWindow.Activate();
        }
    }

    #endregion

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
        // Initialize the component
        InitializeComponent();

        // The DispatcherQueue event loop exits when all XAML windows on a thread are closed
        // https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.dispatchershutdownmode
        DispatcherShutdownMode = DispatcherShutdownMode.OnLastWindowClose;

        // Build the host
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

                // Public API
                services.AddSingleton<IPublicAPIService, PublicAPIService>();

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
                services.AddSingleton<DesktopWidgets3.Contracts.Services.IAppSettingsService, DesktopWidgets3.Services.AppSettingsService>();

                #endregion

                #region Functional Service

                // Widget Dialogs
                services.AddSingleton<IDialogService, DialogService>();

                // Widgets Management
                services.AddSingleton<IWidgetManagerService, WidgetManagerService>();

                // Widgets Resources
                services.AddSingleton<IWidgetResourceService, WidgetResourceService>();

                // System Info
                services.AddSingleton<IHardwareInfoService, HardwareInfoService>();

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
                services.AddTransient<WidgetSettingPage>();
                services.AddTransient<WidgetSettingViewModel>();
                services.AddTransient<WidgetStorePage>();
                services.AddTransient<WidgetStoreViewModel>();

                // Widgets Window Pages
                services.AddTransient<WidgetPage>();
                services.AddTransient<WidgetViewModel>();

                // Overlay Window Pages
                services.AddTransient<EditModeOverlayPage>();
                services.AddTransient<EditModeOverlayViewModel>();

                #endregion

                #region Configurations

                // Local Stettings
                services.Configure<LocalSettingsKeys>(context.Configuration.GetSection(nameof(LocalSettingsKeys)));

                #endregion
            })
            .Build();

        // Initialize core services
        GetService<IAppNotificationService>().Initialize();

        // Configure exception handlers
        UnhandledException += App_UnhandledException;

        // Initialize core extensions after injecting services
        DependencyExtensions.Initialize(GetService<IDependencyService>());
        LogExtensions.Initialize(GetService<ILogger<App>>());
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        HandleAppUnhandledException(e.Exception, false);
    }

	public static void HandleAppUnhandledException(Exception? ex, bool showToastNotification)
    {
        var exceptionString = ExceptionFormatter.FormatExcpetion(ex);

        Debug.WriteLine(exceptionString);

        Debugger.Break();

        LogExtensions.LogError(ClassName, ex, "An unhandled error occurred.");

        if (showToastNotification)
        {
            GetService<IAppNotificationService>().Show(string.Format("AppNotificationUnhandledExceptionPayload".GetLocalized(),
                $"{ex?.ToString()}{Environment.NewLine}"));
        }
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        if (!IsExistWindow && MainWindow is null)
        {
            MainWindow = new MainWindow();
            await GetService<IActivationService>().ActivateMainWindowAsync(args);
            LogExtensions.LogInformation(ClassName, $"App launched. Launch args type: {args.GetType().Name}.");
        }
    }
}
