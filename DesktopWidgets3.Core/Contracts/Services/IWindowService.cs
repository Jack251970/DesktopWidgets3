using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Contracts.Services;

public interface IWindowService
{
    Task ActivateWidgetWindowAsync(object window);

    Task ActivateOverlayWindowAsync(object window);

    Task ActivateBlankWindowAsync(Window window, object? setContent);
}
