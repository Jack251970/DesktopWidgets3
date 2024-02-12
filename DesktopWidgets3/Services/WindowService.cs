using DesktopWidgets3.Helpers;
using Microsoft.UI.Dispatching;
using Windows.UI.ViewManagement;

namespace DesktopWidgets3.Services;

public class WindowService : IWindowService
{
    public Task RegisterWindowEx(BlankWindow window)
    {
        window.settings_ColorValuesChanged += Window_ColorValuesChanged;

        return App.GetService<IActivationService>().ActivateBlankWindowAsync(window);
    }

    // this handles updating the caption button colors correctly when windows system theme is changed while the app is open
    private void Window_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        UIThreadExtensions.DispatcherQueue!.TryEnqueue(DispatcherQueuePriority.High, TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }
}
