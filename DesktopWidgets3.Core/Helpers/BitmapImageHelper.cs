using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace DesktopWidgets3.Core.Helpers;

public class BitmapImageHelper
{
    /// <summary>
    /// Convert image path to <see cref="BitmapImage"/>.
    /// Edit from: https://github.com/microsoft/devhome.
    /// </summary>
    /// <param name="dispatcherQueue">The <see cref="DispatcherQueue"/> to use.</param>
    /// <param name="imagePath">The image path.</param>
    /// <returns>The <see cref="BitmapImage"/>.</returns>
    public static async Task<BitmapImage> ImagePathToBitmapImageAsync(DispatcherQueue dispatcherQueue, string imagePath)
    {
        // Return the bitmap image via TaskCompletionSource. Using WCT's EnqueueAsync does not suffice here, since if
        // we're already on the thread of the DispatcherQueue then it just directly calls the function, with no async involved.
        var completionSource = new TaskCompletionSource<BitmapImage>();
        dispatcherQueue.TryEnqueue(() =>
        {
            var itemImage = new BitmapImage
            {
                UriSource = new Uri(imagePath)
            };
            completionSource.TrySetResult(itemImage);
        });

        var bitmapImage = await completionSource.Task;

        return bitmapImage;
    }

    /// <summary>
    /// Convert <see cref="IRandomAccessStreamReference"/> to <see cref="BitmapImage"/>.
    /// Edit from: https://github.com/microsoft/devhome.
    /// </summary>
    /// <param name="dispatcherQueue">The <see cref="DispatcherQueue"/> to use.</param>
    /// <param name="iconStreamRef">The <see cref="IRandomAccessStreamReference"/>.</param>
    /// <returns>The <see cref="BitmapImage"/>.</returns>
    public static async Task<BitmapImage> RandomAccessStreamToBitmapImageAsync(DispatcherQueue dispatcherQueue, IRandomAccessStreamReference iconStreamRef)
    {
        // Return the bitmap image via TaskCompletionSource. Using WCT's EnqueueAsync does not suffice here, since if
        // we're already on the thread of the DispatcherQueue then it just directly calls the function, with no async involved.
        var completionSource = new TaskCompletionSource<BitmapImage>();
        dispatcherQueue.TryEnqueue(async () =>
        {
            using var bitmapStream = await iconStreamRef.OpenReadAsync();
            var itemImage = new BitmapImage();
            await itemImage.SetSourceAsync(bitmapStream);
            completionSource.TrySetResult(itemImage);
        });

        var bitmapImage = await completionSource.Task;

        return bitmapImage;
    }
}
