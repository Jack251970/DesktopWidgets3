using System;

namespace DesktopWidgets3.Widget.Jack251970.Clock.Extensions;

internal static class DateTimeExtensions
{
    public static bool Equals(this DateTime dateTime1, DateTime dateTime2, EqualsMode equalsMode)
    {
        var result = true;

        if ((equalsMode & EqualsMode.Year) == EqualsMode.Year)
        {
            result = result && dateTime1.Year == dateTime2.Year;
        }

        if ((equalsMode & EqualsMode.Month) == EqualsMode.Month)
        {
            result = result && dateTime1.Month == dateTime2.Month;
        }

        if ((equalsMode & EqualsMode.Day) == EqualsMode.Day)
        {
            result = result && dateTime1.Day == dateTime2.Day;
        }

        if ((equalsMode & EqualsMode.Hour) == EqualsMode.Hour)
        {
            result = result && dateTime1.Hour == dateTime2.Hour;
        }

        if ((equalsMode & EqualsMode.Minute) == EqualsMode.Minute)
        {
            result = result && dateTime1.Minute == dateTime2.Minute;
        }

        if ((equalsMode & EqualsMode.Second) == EqualsMode.Second)
        {
            result = result && dateTime1.Second == dateTime2.Second;
        }

        return result;
    }
}

[Flags]
public enum EqualsMode
{
    Year = 1,
    Month = 2,
    Day = 4,
    Hour = 8,
    Minute = 16,
    Second = 32,
    Date = Year | Month | Day,
    Time = Hour | Minute | Second,
}
