// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Globalization;
using Windows.Globalization;

namespace Files.App.Services.DateTimeFormatter;

internal abstract class AbstractDateTimeFormatter : IDateTimeFormatter
{
	private static readonly CultureInfo cultureInfo
		= ApplicationLanguages.PrimaryLanguageOverride == string.Empty ? CultureInfo.CurrentCulture : new(ApplicationLanguages.PrimaryLanguageOverride);

	public abstract string Name { get; }

	public abstract string ToShortLabel(DateTimeOffset offset);

	public virtual string ToLongLabel(DateTimeOffset offset)
		=> ToShortLabel(offset);

	public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset, GroupByDateUnit unit)
	{
		var now = DateTimeOffset.Now;
		var time = offset.ToLocalTime();

		var diff = now - offset;

		return 0 switch
		{
			_ when now.Date < time.Date
				=> new Label("Future".ToLocalized(), "\uED28", 1000006),
			_ when now.Date == time.Date
				=> new Label("Today".ToLocalized(), "\uE8D1", 1000005),
			_ when now.AddDays(-1).Date == time.Date
				=> new Label("Yesterday".ToLocalized(), "\uE8BF", 1000004),
			_ when diff.Days <= 7 && GetWeekOfYear(now) == GetWeekOfYear(time)
				=> new Label("EarlierThisWeek".ToLocalized(), "\uE8C0", 1000003),
			_ when diff.Days <= 14 && GetWeekOfYear(now.AddDays(-7)) == GetWeekOfYear(time)
				=> new Label("LastWeek".ToLocalized(), "\uE8C0", 1000002),
			_ when now.Year == time.Year && now.Month == time.Month
				=> new Label("EarlierThisMonth".ToLocalized(), "\uE787", 1000001),
			_ when now.AddMonths(-1).Year == time.Year && now.AddMonths(-1).Month == time.Month
				=> new Label("LastMonth".ToLocalized(), "\uE787", 1000000),

			// Group by month
			_ when unit == GroupByDateUnit.Month
				=> new Label(ToString(time, "Y"), "\uE787", time.Year * 100 + time.Month),

			// Group by year
			_ when now.Year == time.Year
				=> new Label("EarlierThisYear".ToLocalized(), "\uEC92", 10001),
			_ when now.AddYears(-1).Year == time.Year
				=> new Label("LastYear".ToLocalized(), "\uEC92", 10000),
			_
				=> new Label(string.Format("YearN".ToLocalized(), time.Year), "\uEC92", time.Year),
		};
	}

    protected static string ToString(DateTimeOffset offset, string format)
        => offset.ToLocalTime().ToString(format);//, cultureInfo);// TODO: Fix bug here!

    private static int GetWeekOfYear(DateTimeOffset t)
	{
		return cultureInfo.Calendar.GetWeekOfYear(t.DateTime, CalendarWeekRule.FirstFullWeek, cultureInfo.DateTimeFormat.FirstDayOfWeek);
	}

	private class Label : ITimeSpanLabel
	{
		public string Text { get; }

		public string Glyph { get; }

		public int Index { get; }

        public Label(string text, string glyph, int index)
        {
            (Text, Glyph, Index) = (text, glyph, index);
        }
    }
}
