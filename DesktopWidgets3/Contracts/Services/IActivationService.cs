using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services;

public interface IActivationService
{
#if SPLASH_SCREEN
    Task<bool> LaunchMainWindowAsync(object activationArgs);
#endif

    Task ActivateMainWindowAsync(object activationArgs);

    Task ActivateWindowAsync(Window window);
}
