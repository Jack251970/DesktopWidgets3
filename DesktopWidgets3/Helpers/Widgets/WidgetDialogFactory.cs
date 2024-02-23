namespace DesktopWidgets3.Helpers.Widgets;

internal static class WidgetDialogFactory
{
    private static readonly IWidgetDialogService WidgetDialogService = App.GetService<IWidgetDialogService>();

    public static async Task<WidgetDialogResult> ShowDeleteWidgetDialog(this WindowEx window)
    {
        var title = "Dialog_DeleteWidget_Title".GetLocalized();
        var content = "Dialog_DeleteWidget_Content".GetLocalized();
        return await WidgetDialogService.ShowTwoButtonDialog(window, title, content);
    }
}
