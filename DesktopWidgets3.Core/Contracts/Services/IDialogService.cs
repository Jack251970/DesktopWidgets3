using WinUIEx;

namespace DesktopWidgets3.Core.Contracts.Services;

public interface IDialogService
{
    void Initialize();

    DialogScreen GetDialogFullScreenWindow();

    Task ShowOneButtonDialogAsync(WindowEx window, string title, string context, string button = null!);

    Task<WidgetDialogResult> ShowTwoButtonDialogAsync(WindowEx window, string title, string context, string leftButton = null!, string rightButton = null!);

    Task<WidgetDialogResult> ShowThreeButtonDialogAsync(WindowEx window, string title, string context, string leftButton = null!, string centerButton = null!, string rightButton = null!);

    Task ShowOneButtonDialogAsync(DialogScreen window, string title, string context, string button = null!);

    Task<WidgetDialogResult> ShowTwoButtonDialogAsync(DialogScreen window, string title, string context, string leftButton = null!, string rightButton = null!);

    Task<WidgetDialogResult> ShowThreeButtonDialogAsync(DialogScreen window, string title, string context, string leftButton = null!, string centerButton = null!, string rightButton = null!);
}
