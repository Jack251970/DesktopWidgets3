namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    void ShowWidget(string widgetType);

    void CloseWidget(string widgetType);

    void CloseAllWidgets();

    IEnumerable<WindowEx> GetWidgets();

    Task SetThemeAsync();
}
