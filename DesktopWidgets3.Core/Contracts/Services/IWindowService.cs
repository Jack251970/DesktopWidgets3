namespace DesktopWidgets3.Core.Contracts.Services;

public interface IWindowService
{
    Task ActivateBlankWindow(BlankWindow window, bool setContent);
}
