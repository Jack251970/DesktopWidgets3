using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.Activation;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Views.Pages;
using DesktopWidgets3.Views.Pages.Widget;
using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IThemeSelectorService themeSelectorService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
    }

    public async Task ActivateMainWindowAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the MainWindow Content.
        if (App.MainWindow!.Content == null)
        {
            var shell = App.GetService<NavShellPage>();
            App.MainWindow.Content = shell is null ? new Frame() : shell;
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        App.MainWindow.Activate();

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

        // Activate the Window.
        window.Activate();

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

        // Activate the Window.
        window.Activate();

        // Execute tasks after activation.
        await StartupAsync(window);
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
