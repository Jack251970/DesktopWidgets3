using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Contracts.Services;

public interface IWindowService
{
    Task ActivateWidgetWindowAsync(Window window);

    Task ActivateOverlayWindowAsync(Window window);

    Task ActivateBlankWindowAsync(Window window, object? setContent);
}
