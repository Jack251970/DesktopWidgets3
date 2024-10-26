namespace DesktopWidgets3.Helpers.Dialog;

internal static class DialogFactory
{
    private static readonly IDialogService WidgetDialogService = DependencyExtensions.GetRequiredService<IDialogService>();

    public static async Task<WidgetDialogResult> ShowDeleteWidgetDialogAsync(WindowEx window)
    {
        var title = "Dialog_DeleteWidget.Title".GetLocalizedString();
        var content = "Dialog_DeleteWidget.Content".GetLocalizedString();
        return await WidgetDialogService.ShowTwoButtonDialogAsync(window, title, content);
    }

    public static async Task ShowDeleteWidgetFullScreenDialogAsync(Func<Task> deleteFunc)
    {
        var title = "Dialog_DeleteWidget.Title".GetLocalizedString();
        var content = "Dialog_DeleteWidget.Content".GetLocalizedString();
        await WidgetDialogService.ShowFullScreenTwoButtonDialogAsync(title, content, func: async (result) =>
        {
            if (result == WidgetDialogResult.Left)
            {
                await deleteFunc();
            }
        });
    }

    public static async Task<WidgetDialogResult> ShowQuitEditModeDialogAsync(WindowEx window)
    {
        var title = "Dialog_SaveCurrentLayout.Title".GetLocalizedString();
        var content = "Dialog_SaveCurrentLayout.Content".GetLocalizedString();
        return await WidgetDialogService.ShowTwoButtonDialogAsync(window, title, content);
    }
}
