// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Data;
using System.Text;

namespace Files.App.Converters;

internal sealed class StringArrayToStringConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
        if (value is not string[] array || array is not string[])
        {
            return string.Empty;
        }

        var str = new StringBuilder();
		foreach (var i in array)
		{
			str.Append(string.Format("{0}; ", i));
		}

		return str.ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		return ((string)value).Split("; ");
	}
}
