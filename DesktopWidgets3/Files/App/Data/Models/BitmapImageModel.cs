// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.Shared.Utils;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.Files.App.Data.Models;

/// <inheritdoc cref="IImage"/>
internal sealed class BitmapImageModel : IImage
{
    public BitmapImage Image
    {
        get;
    }

    public BitmapImageModel(BitmapImage image)
    {
        Image = image;
    }
}
