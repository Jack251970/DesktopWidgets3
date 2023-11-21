namespace DesktopWidgets3.Contracts.Services;

public interface IDataBaseService
{
    void Initialize();

    void AddLockPeriodData(DateTime startTime, DateTime endTime);

    int GetTotalCompleteTimes();

    int GetTotalCompletedMinutes();

    void GetTodayCompletedInfo(out int completeTimes, out int completedMinutes);
}
