namespace DesktopWidgets3.Services;

public class WindowService : IWindowService
{
    public async Task RegisterWindowEx(BlankWindow window)
    {
        await App.GetService<IActivationService>().ActivateBlankWindowAsync(window);
    }
}
