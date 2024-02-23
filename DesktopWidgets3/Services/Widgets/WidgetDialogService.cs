using Windows.UI.Popups;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetDialogService : IWidgetDialogService
{
    private readonly string Ok = "Ok".GetLocalized();
    private readonly string Cancel = "Cancel".GetLocalized();

    public async Task ShowOneButtonDialog(WindowEx window, string title, string context, string button = null!)
    {
        await window.ShowMessageDialogAsync(context, title);
    }

    public async Task<WidgetDialogResult> ShowTwoButtonDialog(WindowEx window, string title, string context, string leftButton = null!, string rightButton = null!)
    {
        leftButton = leftButton is null ? Ok : leftButton;
        rightButton = rightButton is null ? Cancel : rightButton;
        var commands = new List<IUICommand>
        {
            new UICommand(leftButton),
            new UICommand(rightButton)
        };

        var result = await window.ShowMessageDialogAsync(context, commands, title: title);

        if (result.Label == leftButton)
        {
            return WidgetDialogResult.Left;
        }
        else
        {
            return WidgetDialogResult.Right;
        }
    }

    public async Task<WidgetDialogResult> ShowThreeButtonDialog(WindowEx window, string title, string context, string leftButton = null!, string centerButton = null!, string rightButton = null!)
    {
        if (centerButton is null)
        {
            return await ShowTwoButtonDialog(window, title, context, leftButton, rightButton);
        }

        leftButton = leftButton is null ? Ok : leftButton;
        rightButton = rightButton is null ? Cancel : rightButton;
        var commands = new List<IUICommand>
        {
            new UICommand(leftButton),
            new UICommand(centerButton),
            new UICommand(rightButton)
        };

        var result = await window.ShowMessageDialogAsync(context, commands, title: title);

        if (result.Label == leftButton)
        {
            return WidgetDialogResult.Left;
        }
        else if (result.Label == centerButton)
        {
            return WidgetDialogResult.Center;
        }
        else
        {
            return WidgetDialogResult.Right;
        }
    }
}

public enum WidgetDialogResult
{
    Left,
    Center,
    Right
}
