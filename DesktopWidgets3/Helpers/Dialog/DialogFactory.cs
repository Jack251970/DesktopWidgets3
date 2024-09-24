namespace DesktopWidgets3.Helpers.Dialog;

internal static class DialogFactory
{
    private static readonly IDialogService WidgetDialogService = DependencyExtensions.GetRequiredService<IDialogService>();

    public static async Task<WidgetDialogResult> ShowDeleteWidgetDialogAsync(WindowEx? window = null)
    {
        var title = "Dialog_DeleteWidget.Title".GetLocalized();
        var content = "Dialog_DeleteWidget.Content".GetLocalized();
        return await WidgetDialogService.ShowTwoButtonDialogAsync(window, title, content);
    }

    public static async Task<WidgetDialogResult> ShowQuitEditModeDialogAsync(WindowEx? window = null)
    {
        var title = "Dialog_QuitEditMode.Title".GetLocalized();
        var content = "Dialog_QuitEditMode.Content".GetLocalized();
        return await WidgetDialogService.ShowTwoButtonDialogAsync(window, title, content);
    }
}
