// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Data;

namespace DesktopWidgets3.Converts;

internal sealed class DoubleToStringConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not null)
		{
			return value.ToString()!;
		}

		return "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		try
		{
			return double.Parse((string)value);
		}
		catch (FormatException)
		{
			return null!;
		}
	}
}
