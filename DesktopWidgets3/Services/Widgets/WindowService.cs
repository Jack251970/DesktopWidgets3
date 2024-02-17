namespace DesktopWidgets3.Services.Widgets;

internal class WindowService : IWindowService
{
    public async Task ActivateWindow(BlankWindow window)
    {
        await App.GetService<IActivationService>().ActivateBlankWindowAsync(window);
    }
}
