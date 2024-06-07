// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters;

internal sealed class DateTimeOffsetToStringConverter : IValueConverter
{
	private readonly IDateTimeFormatter formatter;

    public DateTimeOffsetToStringConverter(IFolderViewViewModel folderViewViewModel)
    {
        formatter = folderViewViewModel.GetService<IDateTimeFormatter>();
    }

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
