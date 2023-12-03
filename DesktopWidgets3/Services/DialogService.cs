using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using Windows.UI.Popups;

namespace DesktopWidgets3.Services;

public class DialogService : IDialogService
{
    public enum DialogResult
    {
        Left,
        Right
    }

    private readonly string DialogOk = "Dialog_Ok".GetLocalized();
    private readonly string DialogCancel = "Dialog_Cancel".GetLocalized();

    public async Task<DialogResult> ShowDeleteWidgetDialog(WindowEx window)
    {
        var title = "Dialog_DeleteWidget_Title".GetLocalized();
        var content = "Dialog_DeleteWidget_Content".GetLocalized();
        return await ShowTwoButtonDialog(window, title, content);
    }

    private async Task<DialogResult> ShowTwoButtonDialog(WindowEx window, string title, string context, string? leftButton = null, string? rightButton = null)
    {
        leftButton = leftButton is null ? DialogOk : leftButton;
        rightButton = rightButton is null ? DialogCancel : rightButton;

        var commands = new List<IUICommand>
        {
            new UICommand(leftButton),
            new UICommand(rightButton)
        };

        var result = await window.ShowMessageDialogAsync(context, commands, title: title);
        return result.Label == leftButton ? DialogResult.Left : DialogResult.Right;
    }
}
