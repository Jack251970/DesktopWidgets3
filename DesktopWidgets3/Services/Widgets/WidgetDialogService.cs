using Windows.UI.Popups;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetDialogService : IWidgetDialogService
{
    private readonly string Ok = "Ok".GetLocalized();
    private readonly string Cancel = "Cancel".GetLocalized();

    public async Task<WidgetDialogResult> ShowDeleteWidgetDialog(WindowEx window)
    {
        var title = "Dialog_DeleteWidget_Title".GetLocalized();
        var content = "Dialog_DeleteWidget_Content".GetLocalized();
        return await ShowTwoButtonDialog(window, title, content);
    }

    private async Task<WidgetDialogResult> ShowTwoButtonDialog(WindowEx window, string title, string context, string? leftButton = null, string? rightButton = null)
    {
        leftButton = leftButton is null ? Ok : leftButton;
        rightButton = rightButton is null ? Cancel : rightButton;

        var commands = new List<IUICommand>
        {
            new UICommand(leftButton),
            new UICommand(rightButton)
        };

        var result = await window.ShowMessageDialogAsync(context, commands, title: title);
        return result.Label == leftButton ? WidgetDialogResult.Left : WidgetDialogResult.Right;
    }
}

public enum WidgetDialogResult
{
    Left,
    Right
}
