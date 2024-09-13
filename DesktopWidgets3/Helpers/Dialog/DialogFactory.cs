namespace DesktopWidgets3.Helpers.Dialog;

internal static class DialogFactory
{
    private static readonly IDialogService WidgetDialogService = DependencyExtensions.GetRequiredService<IDialogService>();

    public static async Task<WidgetDialogResult> ShowDeleteWidgetDialogAsync(this WindowEx window)
    {
        var title = "Dialog_DeleteWidget.Title".GetLocalized();
        var content = "Dialog_DeleteWidget.Content".GetLocalized();
        return await WidgetDialogService.ShowTwoButtonDialogAsync(window, title, content);
    }
}
