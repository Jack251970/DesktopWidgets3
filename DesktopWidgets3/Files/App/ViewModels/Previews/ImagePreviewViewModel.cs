// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.App.Extensions;
using DesktopWidgets3.Files.App.Utils;
using DesktopWidgets3.Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DesktopWidgets3.Files.App.ViewModels.Previews;

public class ImagePreviewViewModel : BasePreviewModel
{
    private ImageSource imageSource;
    public ImageSource ImageSource
    {
        get => imageSource;
        private set => SetProperty(ref imageSource, value);
    }

    public ImagePreviewViewModel(ListedItem item)
        : base(item)
    {
    }

    // FILESTODO: Use existing helper mothods
    public static bool ContainsExtension(string extension)
        => extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tiff" or ".ico" or ".webp";

    public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
    {
        using IRandomAccessStream stream = await Item.ItemFile.OpenAsync(FileAccessMode.Read);

        await DesktopWidgets3.App.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
        {
            BitmapImage bitmap = new();
            await bitmap.SetSourceAsync(stream);
            ImageSource = bitmap;
        });

        return new List<FileProperty>();
    }
}