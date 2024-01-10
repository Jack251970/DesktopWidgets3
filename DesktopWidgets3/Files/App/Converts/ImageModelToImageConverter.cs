// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Models;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters;

internal sealed class ImageModelToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return value is BitmapImageModel bitmapImageModel ? bitmapImageModel.Image : (object?)null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
