using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3.Contracts.Services;

public interface IActivationService
{
    Task ActivateMainWindowAsync(object activationArgs);

    Task ActivateWidgetWindowAsync(WidgetWindow window, object widgetSettings);

    Task ActivateOverlayWindowAsync(OverlayWindow window);
}
