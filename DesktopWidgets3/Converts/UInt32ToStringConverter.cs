// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Data;

namespace DesktopWidgets3.Converts;

internal sealed class UInt32ToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value != null ? value.ToString()! : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        try
        {
            return uint.Parse((string)value);
        }
        catch (FormatException)
        {
            return null!;
        }
    }
}
