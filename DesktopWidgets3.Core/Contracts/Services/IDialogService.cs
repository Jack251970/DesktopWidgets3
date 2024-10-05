using WinUIEx;

namespace DesktopWidgets3.Core.Contracts.Services;

public interface IDialogService
{
    Task ShowOneButtonDialogAsync(WindowEx window, string title, string context, string button = null!);

    Task<WidgetDialogResult> ShowTwoButtonDialogAsync(WindowEx window, string title, string context, string leftButton = null!, string rightButton = null!);

    Task<WidgetDialogResult> ShowThreeButtonDialogAsync(WindowEx window, string title, string context, string leftButton = null!, string centerButton = null!, string rightButton = null!);
}
