using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services.Widgets;

internal class WindowService : IWindowService
{
    public async Task ActivateWidgetWindowAsync(Window window)
    {
        if (window is WidgetWindow widgetWindow)
        {
            await App.GetService<IActivationService>().ActivateWidgetWindowAsync(widgetWindow);
        }
    }

    public async Task ActivateOverlayWindowAsync(Window window)
    {
        if (window is OverlayWindow overlayWindow)
        {
            await App.GetService<IActivationService>().ActivateOverlayWindowAsync(overlayWindow);
        }
    }

    public async Task ActivateBlankWindowAsync(Window window, object? setContent)
    {
        if (setContent is bool setContentBool)
        {
            await App.GetService<IActivationService>().ActivateWindowAsync(window, setContentBool);
        }
        else
        {
            await App.GetService<IActivationService>().ActivateWindowAsync(window);
        }
    }
}
