// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters;

public sealed class DateTimeOffsetToStringConverter : IValueConverter
{
	private static readonly IDateTimeFormatter formatter = DependencyExtensions.GetService<IDateTimeFormatter>();

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		return value is null
			? string.Empty
			: formatter.ToLongLabel((DateTimeOffset)value);
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		try
		{
			return DateTimeOffset.Parse((string)value);
		}
		catch (FormatException)
		{
			return null!;
		}
	}
}
