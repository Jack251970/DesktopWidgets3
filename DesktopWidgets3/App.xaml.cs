using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3;

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
            MainWindow.BringToFront();
        }
    }

    #endregion

    #region Edit Mode Window

    public static EditModeWindow EditModeWindow { get; set; } = null!;

    #endregion

    #region Constructor

    public App()
    {
        // Check if app is already running
        if (SystemHelper.IsWindowExist(null, ConstantHelper.AppAppDisplayName, true))
        {
            IsExistWindow = true;
            Current.Exit();
            return;
        }

        // Initialize the component
        InitializeComponent();

        // Initialize core helpers before services
        LocalSettingsHelper.Initialize();

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

                // Backdrop Management
                services.AddSingleton<IBackdropSelectorService, BackdropSelectorService>();

                // Dependency Injection
                services.AddSingleton<IDependencyService, DependencyService>();

                // Dialog Managment
                services.AddSingleton<IDialogService, DialogService>();

                // Main window: Allow access to the main window
                // from anywhere in the application.
                services.AddSingleton(_ => MainWindow);

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
                services.AddSingleton<DesktopWidgets3.Contracts.Services.IAppSettingsService, DesktopWidgets3.Services.AppSettingsService>();

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

                services.AddSingleton<ILogService, LogService>();

                services.AddSingleton<ISettingsService, SettingsService>();

                services.AddSingleton<IThemeService, ThemeService>();

                services.AddSingleton<IWidgetService, WidgetService>();

                #endregion

                #endregion

                #region Views & ViewModels

                // Main Window Pages
                services.AddTransient<NavShellPage>();
                services.AddTransient<NavShellViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddTransient<HomePage>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<DashboardPage>();
                services.AddTransient<WidgetStorePage>();
                services.AddTransient<WidgetStoreViewModel>();

                // Widgets Window Pages
                services.AddTransient<WidgetSettingPage>();
                services.AddTransient<WidgetSettingViewModel>();
                services.AddTransient<WidgetViewModel>();

                #endregion

                #region Configurations

                // Local Stettings
                services.Configure<LocalSettingsKeys>(context.Configuration.GetSection(nameof(LocalSettingsKeys)));

                #endregion

                #region DevHome.Dashboard

                // DevHome.Dashboard Services
                services.AddDashboard(context);

                #endregion
            })
            .Build();

        // Configure exception handlers
        UnhandledException += App_UnhandledException;

        // Initialize core extensions after services
        DependencyExtensions.Initialize(GetService<IDependencyService>());
        LogExtensions.Initialize(GetService<ILogger<App>>());

        // Initialize core services
        GetService<IAppSettingsService>().Initialize();
        GetService<IAppNotificationService>().Initialize();

        // Initialize core helpers after services
        AppLanguageHelper.Initialize();
        ApplicationExtensionHost.Initialize(this);
    }

    #endregion

    #region App Lifecycle

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (IsExistWindow)
        {
            return;
        }

        base.OnLaunched(args);

        // Ensure the current window is active
        if (MainWindow != null)
        {
            return;
        }

        LogExtensions.LogInformation(ClassName, $"App launched. Launch args type: {args.GetType().Name}.");

        // Create main window
        MainWindow = new MainWindow();
        await GetService<IActivationService>().ActivateMainWindowAsync(args);
        
        // Create edit mode window
        EditModeWindow = WindowsExtensions.CreateWindow<EditModeWindow>();
        await GetService<IActivationService>().ActivateWindowAsync(EditModeWindow);

        // Initialize dialog service
        GetService<IDialogService>().Initialize();

        // Initialize widgets
        await GetService<IWidgetResourceService>().InitalizeAsync();
        GetService<IWidgetManagerService>().InitializePinnedWidgets();

        // TEST: Add widget dialog.
        /*var dialog = new AddWidgetDialog()
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app.
            XamlRoot = MainWindow.Content.XamlRoot,
        };
        await dialog.ShowAsync();*/
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var ex = e.Exception;
        var exceptionString = ExceptionFormatter.FormatExcpetion(ex);

        Debug.WriteLine(exceptionString);

        Debugger.Break();

        LogExtensions.LogError(ClassName, ex, "An unhandled error occurred.");

        GetService<IAppNotificationService>().TryShow(
            string.Format("AppNotificationUnhandledExceptionPayload".GetLocalized(),
            $"{ex?.ToString()}{Environment.NewLine}"));
    }

    public static async new void Exit()
    {
        LogExtensions.LogInformation(ClassName, "Exit current application.");

        // Close all widgets
        await GetService<IWidgetManagerService>().CloseAllWidgetsAsync();

        // Close all windows
        await WindowsExtensions.CloseAllWindowsAsync();

        // Dispose all widgets
        await GetService<IWidgetResourceService>().DisposeWidgetsAsync();

        // Dispose all extension assemblies
        WidgetsLoader.DisposeExtensionAssemblies();

        // Unregister app notification service
        GetService<IAppNotificationService>().Unregister();

        Current.Exit();
    }

    public static void RestartApplication()
    {
        // Get the path to the executable
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;

        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
        {
            // Start a new instance of the application
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });

            // exit the current application
            CanCloseWindow = true;
            MainWindow.Close();
        }
    }

    #endregion
}
