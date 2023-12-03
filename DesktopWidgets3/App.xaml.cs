using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

using DesktopWidgets3.Activation;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Core.Contracts.Services;
using DesktopWidgets3.Core.Services;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models;
using DesktopWidgets3.Notifications;
using DesktopWidgets3.Services;
using DesktopWidgets3.ViewModels.Pages;
using DesktopWidgets3.Views.Pages;
using DesktopWidgets3.Views.Windows;
using DesktopWidgets3.ViewModels.Pages.Widget.Clock;
using DesktopWidgets3.Views.Pages.Widget.Clock;
using DesktopWidgets3.Views.Pages.Widget.FolderView;
using DesktopWidgets3.ViewModels.Pages.Widget.FolderView;
using DesktopWidgets3.Views.Pages.Widget;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.ViewModels.Pages.Widget.Settings;
using DesktopWidgets3.Views.Pages.Widget.Settings;

namespace DesktopWidgets3;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost? Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host!.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static MainWindow? MainWindow { get; set; }

    public static UIElement? AppTitleBar { get; set; }
    public static UIElement? AppTitleBarText { get; set; }

    public static bool CanCloseWindow { get; set; }
    private static bool IsExistWindow { get; set; }

    public App()
    {
        // Check if app is already running
        if (SystemHelper.IsWindowExist(null, "AppDisplayName".GetLocalized(), true))
        {
            IsExistWindow = true;
            Current.Exit();
            return;
        }

        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
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
            services.AddSingleton<IAppSettingsService, AppSettingsService>();

            #endregion

            #region Functional Service

            // Dialogs
            services.AddSingleton<IDialogService, DialogService>();

            // Timers
            services.AddSingleton<ITimersService, TimersService>();

            // Widgets Management
            services.AddSingleton<IWidgetManagerService, WidgetManagerService>();

            // Widgets Resources
            services.AddSingleton<IWidgetResourceService, WidgetResourceService>();

            // Sink Windows
            services.AddTransient<IWindowSinkService, WindowSinkService>();

            #endregion

            #region Views & ViewModels

            // MainwWindow Pages
            services.AddTransient<NavShellPage>();
            services.AddTransient<NavShellViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<HomePage>();
            services.AddTransient<WidgetSettingViewModel>();
            services.AddTransient<WidgetSettingPage>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<DashboardPage>();
            services.AddTransient<ClockSettingsViewModel>();
            services.AddTransient<ClockSettingsPage>();
            services.AddTransient<FolderViewSettingsViewModel>();
            services.AddTransient<FolderViewSettingsPage>();

            // Widgets Window Pages
            services.AddTransient<FrameShellPage>();
            services.AddTransient<FrameShellViewModel>();
            services.AddTransient<EditModeOverlayPage>();
            services.AddTransient<EditModeOverlayViewModel>();
            services.AddTransient<ClockViewModel>();
            services.AddTransient<ClockPage>();
            services.AddTransient<FolderViewViewModel>();
            services.AddTransient<FolderViewPage>();

            #endregion

            #region Configurations

            // Local Storage
            services.Configure<LocalSettingsKeys>(context.Configuration.GetSection(nameof(LocalSettingsKeys)));
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

            #endregion
        }).
        Build();

        GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    // For a desktop application (unpackaged mode), args.Arguments is always string.Empty.
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        if (!IsExistWindow)
        {
            MainWindow = new MainWindow();
            await GetService<IActivationService>().ActivateMainWindowAsync(args);
        }
    }

    public static void ShowMainWindow(bool front)
    {
        MainWindow!.Show(true);
        MainWindow!.Activate();
        if (front)
        {
            MainWindow.BringToFront();
        }
    }
}
