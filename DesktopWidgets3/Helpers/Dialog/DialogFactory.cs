namespace DesktopWidgets3.Helpers.Dialog;

internal static class DialogFactory
{
    private static readonly IDialogService WidgetDialogService = App.GetService<IDialogService>();

    public static async Task<WidgetDialogResult> ShowDeleteWidgetDialog(this WindowEx window)
    {
        var title = "Dialog_DeleteWidget_Title".GetLocalized();
        var content = "Dialog_DeleteWidget_Content".GetLocalized();
        return await WidgetDialogService.ShowTwoButtonDialog(window, title, content);
    }
}
