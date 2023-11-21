using Microsoft.Data.Sqlite;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models;

namespace DesktopWidgets3.Services;

public class DataBaseService : IDataBaseService
{
    private const int lockPeriodsDbVersion = 1;
    private const string timeFormat = @"yyyy-MM-dd HH:mm:ss";

    private readonly string dataBasePath;
    private readonly string dataBaseName = "DataBase.db";

    private readonly string lockPeriodsKey = "lockPeriods";
    private readonly List<LockPeriodData> lockPeriodList = new();

    public DataBaseService(ILocalSettingsService localSettingsService)
    {
        var path = localSettingsService.GetApplicationDataFolder();
        dataBasePath = Path.Combine(path, dataBaseName);
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection($@"Data Source={dataBasePath}");
        connection.Open();

        if (File.Exists(dataBasePath))
        {
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{lockPeriodsKey}'";

            var result = command.ExecuteScalar();

            if (result != null && result.ToString() == lockPeriodsKey)
            {
                LoadAllLockPeriodData(connection);
            }
            else
            {
                CreateLockPeriods(connection);
            }
        }
        else
        {
            CreateLockPeriods(connection);
        }
    }

    private void LoadAllLockPeriodData(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText =
        @$"
            SELECT id, version, startTime, endTime
            FROM {lockPeriodsKey}
        ";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var version = reader.GetInt32(1);
            var startTime = reader.GetDateTime(2);
            var endTime = reader.GetDateTime(3);

            var lockPeriodData = new LockPeriodData
            {
                ID = id,
                Version = version,
                StartTime = startTime,
                EndTime = endTime
            };

            lockPeriodList.Add(lockPeriodData);
        }
    }

    private void CreateLockPeriods(SqliteConnection connection)
    {
        var createCommand = connection.CreateCommand();
        createCommand.CommandText =
        @$"
            CREATE TABLE {lockPeriodsKey} (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                version INTEGER NOT NULL,
                startTime TEXT NOT NULL,
                endTime TEXT NOT NULL
            );
        ";
        createCommand.ExecuteNonQuery();
    }

    public void AddLockPeriodData(DateTime startTime, DateTime endTime)
    {
        using var connection = new SqliteConnection($@"Data Source={dataBasePath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
        @$"
            INSERT INTO {lockPeriodsKey} (version, startTime, endTime)
            VALUES ($version, $startTime, $endTime)
        ";
        command.Parameters.AddWithValue("$version", lockPeriodsDbVersion);
        command.Parameters.AddWithValue("$startTime", startTime.ToString(timeFormat));
        command.Parameters.AddWithValue("$endTime", endTime.ToString(timeFormat));
        command.ExecuteNonQuery();

        command.CommandText =
        @"
            SELECT last_insert_rowid()
        ";
        var id = (long)command.ExecuteScalar()!;

        lockPeriodList.Add(new LockPeriodData
        {
            ID = (int)id,
            Version = lockPeriodsDbVersion,
            StartTime = startTime,
            EndTime = endTime
        });
    }

    public int GetTotalCompleteTimes()
    {
        return lockPeriodList.Count;
    }

    public int GetTotalCompletedMinutes()
    {
        var periodMinute = 0;
        for (var i = 0; i < lockPeriodList.Count; i++)
        {
            var lockPeriodData = lockPeriodList[i];
            var timePeriod = lockPeriodData.TimePeriod;
            if (timePeriod == null)
            {
                var startTime = lockPeriodData.StartTime;
                var endTime = lockPeriodData.EndTime;
                timePeriod = (int)(endTime - startTime).TotalMinutes;
                lockPeriodData.TimePeriod = timePeriod;
            }
            periodMinute += (int)timePeriod;
        }

        return periodMinute;
    }

    public void GetTodayCompletedInfo(out int completeTimes, out int completedMinutes)
    {
        completeTimes = 0;
        completedMinutes = 0;

        var startToday = DateTime.Now.Date;
        var endToday = startToday.AddDays(1).AddSeconds(-1);
        foreach (var lockPeriodData in lockPeriodList)
        {
            var startTime = lockPeriodData.StartTime;
            var endTime = lockPeriodData.EndTime;

            var intersectionStart = startTime > startToday ? startTime : startToday;
            var intersectionEnd = endTime < endToday ? endTime : endToday;

            if (DateTime.Compare(intersectionStart, intersectionEnd) <= 0)
            {
                var timePeriod = (int)(intersectionEnd - intersectionStart).TotalMinutes;
                timePeriod = timePeriod >= 0 ? timePeriod : 0;

                completeTimes++;
                completedMinutes += timePeriod;
            }
        }
    }
}
