// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Services.DateTimeFormatter;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converts;

internal sealed class DateTimeOffsetToStringConverter : IValueConverter
{
    private static readonly IDateTimeFormatter formatter = DesktopWidgets3.App.GetService<IDateTimeFormatter>();

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
