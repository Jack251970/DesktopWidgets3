using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    void ShowWidget(string widgetType);

    void CloseWidget(string widgetType);

    void CloseAllWidgets();

    IEnumerable<BlankWindow> GetWidgets();

    Task SetThemeAsync();
}
