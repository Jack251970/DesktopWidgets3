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

    public static async Task ShowDeleteWidgetFullScreenDialogAsync(Func<Task> deleteFunc)
    {
        var title = "Dialog_DeleteWidget.Title".GetLocalized();
        var content = "Dialog_DeleteWidget.Content".GetLocalized();
        var fullScreen = WidgetDialogService.GetDialogFullScreenWindow();
        await fullScreen.EnqueueOrInvokeAsync(async (window) =>
        {
            window.Show();
            if (await WidgetDialogService.ShowTwoButtonDialogAsync(window, title, content) == WidgetDialogResult.Left)
            {
                await deleteFunc();
            }
            window.Hide();
        });
    }

    public static async Task<WidgetDialogResult> ShowQuitEditModeDialogAsync(WindowEx window)
    {
        var title = "Dialog_SaveCurrentLayout.Title".GetLocalized();
        var content = "Dialog_SaveCurrentLayout.Content".GetLocalized();
        return await WidgetDialogService.ShowTwoButtonDialogAsync(window, title, content);
    }
}
