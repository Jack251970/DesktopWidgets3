using DesktopWidgets3.Helpers;
using Windows.UI.Popups;

namespace DesktopWidgets3.Services;

public class WidgetDialogService : IWidgetDialogService
{
    public enum WidgetDialogResult
    {
        Left,
        Right
    }

    private readonly string Ok = "Ok".ToLocalized();
    private readonly string Cancel = "Cancel".ToLocalized();

    public async Task<WidgetDialogResult> ShowDeleteWidgetDialog(WindowEx window)
    {
        var title = "Dialog_DeleteWidget_Title".ToLocalized();
        var content = "Dialog_DeleteWidget_Content".ToLocalized();
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
