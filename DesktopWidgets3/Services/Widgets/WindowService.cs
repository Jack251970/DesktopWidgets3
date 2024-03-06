namespace DesktopWidgets3.Services.Widgets;

internal class WindowService : IWindowService
{
    public async Task ActivateBlankWindow(BlankWindow window, bool setContent)
    {
        await App.GetService<IActivationService>().ActivateBlankWindowAsync(window, setContent);
    }
}
