using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Contracts.Services;

public interface ITimersService
{
    void AddTimerAction(WidgetType type, Action timeDelegate);

    void RemoveTimerAction(WidgetType type, Action timeDelegate);

    void StartTimer(WidgetType type);

    void StopTimer(WidgetType type);
}
