using static DesktopWidgets3.Services.WidgetDialogService;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetDialogService
{
    Task<WidgetDialogResult> ShowDeleteWidgetDialog(WindowEx window);
}
