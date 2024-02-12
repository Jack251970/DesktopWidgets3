using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.Activation;
using DesktopWidgets3.Views.Pages;
using DesktopWidgets3.Views.Pages.Widget;
using DesktopWidgets3.Views.Windows;
using DesktopWidgets3.Helpers;
using Windows.UI.ViewManagement;

namespace DesktopWidgets3.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IThemeSelectorService _themeSelectorService;
    private UIElement? _shell = null;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _appSettingsService = appSettingsService;
        _themeSelectorService = themeSelectorService;
    }

    public async Task ActivateMainWindowAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();
        await _appSettingsService.InitializeAsync();

        // Set the MainWindow Content.
        if (App.MainWindow.Content == null)
        {
            _shell = App.GetService<NavShellPage>();
            App.MainWindow.Content = _shell ?? new Frame();
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        if (!_appSettingsService.SilentStart)
        {
            App.MainWindow.Activate();
        }

        // Execute tasks after activation.
        await StartupAsync(App.MainWindow);
    }

    public async Task ActivateWidgetWindowAsync(WidgetWindow window, object widgetSettings)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the Window Content and handle widget settings.
        if (window.Content == null)
        {
            var shell = App.GetService<FrameShellPage>();
            window.Content = shell is null ? new Frame() : shell;
            shell?.ViewModel.WidgetNavigationService.NavigateTo(window.WidgetType, widgetSettings);
        }

        // Execute tasks after activation.
        await StartupAsync(window);
    }

    public async Task ActivateOverlayWindowAsync(OverlayWindow window)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the Window Content.
        if (window.Content == null)
        {
            window.Content = new Frame();
        }

        // Execute tasks after activation.
        await StartupAsync(window);
    }

    public async Task ActivateBlankWindowAsync(BlankWindow window)
    {
        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        window.settings_ColorValuesChanged += Window_ColorValuesChanged;

        // Execute tasks before activation.
        await InitializeAsync();

        // Set the Window Content.
        if (window.Content == null)
        {
            window.Content = new Frame();
        }

        // Execute tasks after activation.
        await StartupAsync(window);
    }

    // this handles updating the caption button colors correctly when windows system theme is changed while the app is open
    private void Window_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        UIThreadExtensions.DispatcherQueue!.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync(Window window)
    {
        await _themeSelectorService.SetRequestedThemeAsync(window);
        await Task.CompletedTask;
    }
}
