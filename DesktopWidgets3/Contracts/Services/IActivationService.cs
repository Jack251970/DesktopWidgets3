using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services;

public interface IActivationService
{
    Task ActivateMainWindowAsync(object activationArgs);

    Task ActivateWidgetWindowAsync(WidgetWindow window);

    Task ActivateOverlayWindowAsync(OverlayWindow window);

    Task ActivateWindowAsync(Window window, bool setContent = false);
}
