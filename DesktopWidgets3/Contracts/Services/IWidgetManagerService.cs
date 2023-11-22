namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    void ShowWidget(string widgetType);

    void CloseAllWidgets();
}
