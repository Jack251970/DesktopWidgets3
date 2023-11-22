using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services;

public interface IActivationService
{
    Task ActivateMainWindowAsync(object activationArgs);

    Task ActivateWidgetWindowAsync(Window window);
}
