namespace DesktopWidgets3.Helpers.Dialog;

internal static class DialogFactory
{
    private static readonly IDialogService WidgetDialogService = DependencyExtensions.GetRequiredService<IDialogService>();

    public static async Task<WidgetDialogResult> ShowDeleteWidgetDialogAsync(WindowEx window)
    {
        var title = "Dialog_DeleteWidget.Title".GetLocalized();
        var content = "Dialog_DeleteWidget.Content".GetLocalized();
        return await WidgetDialogService.ShowTwoButtonDialogAsync(window, title, content);
    }

    public static async Task ShowDeleteWidgetFullScreenDialogAsync(Func<Task> func)
    {
        var title = "Dialog_DeleteWidget.Title".GetLocalized();
        var content = "Dialog_DeleteWidget.Content".GetLocalized();
        var window = App.FullScreenWindow;
        await App.FullScreenWindow.EnqueueOrInvokeAsync(async (window) =>
        {
            window.Show();
            if (await ShowDeleteWidgetDialogAsync(window) == WidgetDialogResult.Left)
            {
                await func();
            }
            window.Hide();
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);
    }

    public static async Task<WidgetDialogResult> ShowQuitEditModeDialogAsync(WindowEx window)
    {
        var title = "Dialog_SaveCurrentLayout.Title".GetLocalized();
        var content = "Dialog_SaveCurrentLayout.Content".GetLocalized();
        return await WidgetDialogService.ShowTwoButtonDialogAsync(window, title, content);
    }
}
