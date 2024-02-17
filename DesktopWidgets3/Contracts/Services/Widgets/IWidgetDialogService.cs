namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetDialogService
{
    Task<WidgetDialogResult> ShowDeleteWidgetDialog(WindowEx window);
}
