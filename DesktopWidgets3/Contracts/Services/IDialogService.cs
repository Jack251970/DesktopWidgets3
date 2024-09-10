namespace DesktopWidgets3.Contracts.Services;

public interface IDialogService
{
    Task ShowOneButtonDialog(WindowEx window, string title, string context, string button = null!);

    Task<WidgetDialogResult> ShowTwoButtonDialog(WindowEx window, string title, string context, string leftButton = null!, string rightButton = null!);

    Task<WidgetDialogResult> ShowThreeButtonDialog(WindowEx window, string title, string context, string leftButton = null!, string centerButton = null!, string rightButton = null!);
}
