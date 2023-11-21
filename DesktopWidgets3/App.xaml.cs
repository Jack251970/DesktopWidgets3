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
using DesktopWidgets3.ViewModels.SubPages;
using DesktopWidgets3.Views.Pages;
using DesktopWidgets3.Views.SubPages;
using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost? Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host!.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx? MainWindow { get; set; }
    public static void ShowMainWindow(bool front)
    {
        MainWindow!.Show(true);
        MainWindow!.Activate();
        if (front)
        {
            MainWindow.BringToFront();
        }
    }

    private static bool closeWindow = false;
    public static bool CheckCanCloseWindow()
    {
        if (closeWindow == false)
        {
            return false;
        }
        closeWindow = false;
        return true;
    }
    public static void EnableCloseWindow()
    {
        closeWindow = true;
    }

    private static bool existWindow = false;

    public static UIElement? AppTitleBar { get; set; }
    public static UIElement? AppTitleBarText { get; set; }

    public App()
    {
        if (SystemHelper.IsWindowExist(null, "AppDisplayName".GetLocalized(), true))
        {
            existWindow = true;
            Current.Exit();
            return;
        }

        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddSingleton<ISubPageService, SubPageService>();
            services.AddSingleton<ISubNavigationService, SubNavigationService>();

            services.AddSingleton<ITimersService, TimersService>();

            services.AddSingleton<IDataBaseService, DataBaseService>();

            // unable to register event in SystemEvents?
            // services.AddSingleton<ISessionSwitchService, SessionSwitchService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels of Pages
            // TODO: Register your services of new pages here.
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<HomePage>();
            services.AddTransient<TimingViewModel>();
            services.AddTransient<TimingPage>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<BlockListViewModel>();
            services.AddTransient<BlockListPage>();
            services.AddTransient<StatisticViewModel>();
            services.AddTransient<StatisticPage>();

            // View and ViewModels of SubPages
            // TODO: Register your services of new sub pages here.
            // Subpages of timing page
            services.AddTransient<StartSettingViewModel>();
            services.AddTransient<StartSettingPage>();
            services.AddTransient<SetMinutesViewModel>();
            services.AddTransient<SetMinutesPage>();
            services.AddTransient<MainTimingViewModel>();
            services.AddTransient<MainTimingPage>();
            services.AddTransient<CompleteTimingViewModel>();
            services.AddTransient<CompleteTimingPage>();

            // Configuration
            services.Configure<LocalSettingsKeys>(context.Configuration.GetSection(nameof(LocalSettingsKeys)));
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        GetService<IDataBaseService>().Initialize();
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

        if (!existWindow)
        {
            MainWindow = new MainWindow();

            await GetService<IActivationService>().ActivateAsync(args);
        }
    }
}
