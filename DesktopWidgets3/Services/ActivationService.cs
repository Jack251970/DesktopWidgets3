using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Services;

internal class ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService) : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler = defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers = activationHandlers;
    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly IThemeSelectorService _themeSelectorService = themeSelectorService;
    private UIElement? _shell = null;

    public async Task ActivateMainWindowAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the MainWindow Content.
        if (App.MainWindow.Content == null)
        {
            _shell = DependencyExtensions.GetRequiredService<NavShellPage>();
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

    public async Task ActivateWindowAsync(Window window)
    {
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

        await _appSettingsService.InitializeAsync().ConfigureAwait(false);

        await Task.CompletedTask;
    }

    private async Task StartupAsync(Window window)
    {
        await _themeSelectorService.SetRequestedThemeAsync(window);

        await Task.CompletedTask;
    }
}
