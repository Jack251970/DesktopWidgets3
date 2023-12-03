using static DesktopWidgets3.Services.DialogService;

namespace DesktopWidgets3.Contracts.Services;

public interface IDialogService
{
    Task<DialogResult> ShowDeleteWidgetDialog(WindowEx window);
}
