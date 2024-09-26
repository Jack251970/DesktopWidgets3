using Windows.UI.Popups;

namespace DesktopWidgets3.Services;

internal class DialogService : IDialogService
{
    private readonly string Ok = "Ok".GetLocalized();
    private readonly string Cancel = "Cancel".GetLocalized();

    public async Task ShowOneButtonDialogAsync(WindowEx window, string title, string context, string button = null!)
    {
        await ShowMessageDialogAsync(window, context, null, title: title);
    }

    public async Task<WidgetDialogResult> ShowTwoButtonDialogAsync(WindowEx window, string title, string context, string leftButton = null!, string rightButton = null!)
    {
        leftButton = leftButton is null ? Ok : leftButton;
        rightButton = rightButton is null ? Cancel : rightButton;
        var commands = new List<IUICommand>
        {
            new UICommand(leftButton),
            new UICommand(rightButton)
        };

        var result = await ShowMessageDialogAsync(window, context, commands, title: title);

        if (result == null)
        {
            return WidgetDialogResult.Unknown;
        }

        if (result.Label == leftButton)
        {
            return WidgetDialogResult.Left;
        }
        else
        {
            return WidgetDialogResult.Right;
        }
    }

    public async Task<WidgetDialogResult> ShowThreeButtonDialogAsync(WindowEx window, string title, string context, string leftButton = null!, string centerButton = null!, string rightButton = null!)
    {
        if (centerButton is null)
        {
            return await ShowTwoButtonDialogAsync(window, title, context, leftButton, rightButton);
        }

        leftButton = leftButton is null ? Ok : leftButton;
        rightButton = rightButton is null ? Cancel : rightButton;
        var commands = new List<IUICommand>
        {
            new UICommand(leftButton),
            new UICommand(centerButton),
            new UICommand(rightButton)
        };

        var result = await ShowMessageDialogAsync(window, context, commands, title: title);

        if (result == null)
        {
            return WidgetDialogResult.Unknown;
        }

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

    public async Task HideFullScreenDialogAsync()
    {
        await App.FullScreenWindow.EnqueueOrInvokeAsync((window) => window.Hide(), Microsoft.UI.Dispatching.DispatcherQueuePriority.High);
    }

    private static async Task<IUICommand> ShowMessageDialogAsync(WindowEx window, string content, IList<IUICommand>? commands, uint defaultCommandIndex = 0u, uint cancelCommandIndex = 1u, string title = "")
    {
        return await window.ShowMessageDialogAsync(content, commands, defaultCommandIndex, cancelCommandIndex, title);
    }
}

public enum WidgetDialogResult
{
    Left,
    Center,
    Right,
    Unknown
}
