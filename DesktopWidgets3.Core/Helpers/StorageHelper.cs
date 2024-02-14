using Windows.Storage;
using Windows.Storage.Pickers;

namespace DesktopWidgets3.Core.Helpers;

public class StorageHelper
{
    private static readonly string[] UriPrefixes = {
        "ms-appx://", "ms-appdata://", "ms-appdata:///", "ms-appx:///",
        "ms-appx-web://", "ms-appx-web:///",
        "ms-appdata:///local/", "ms-appdata:///temp/", "ms-appdata:///roaming/",
        "ms-appdata:///localcache/", "ms-appdata:///tempstate/", "ms-appdata:///localstate/",
        "ms-appdata:///roamingstate/", "ms-appdata:///localcache/", "ms-appdata:///tempstate/",
        "ms-appdata:///localstate/", "ms-appdata:///roamingstate/", "ms-appdata:///localcache/",
        "ms-appdata:///tempstate/", "ms-appdata:///localstate/", "ms-appdata:///roamingstate/"
    };

    public static Task<StorageFile> GetStorageFile(string uriPath, string? path = null)
    {
        if (RuntimeHelper.IsMSIX)
        {
            return StorageFile.GetFileFromApplicationUriAsync(new Uri(uriPath)).AsTask();
        }
        else
        {
            path ??= uriPath;
            foreach (var prefix in UriPrefixes)
            {
                if (path.StartsWith(prefix))
                {
                    // Remove the prefix from the URI
                    path = path[prefix.Length..];
                    break; // Stop checking once a matching prefix is found
                }
            }

            path = path.Replace('/', '\\');
            if (path.StartsWith("\\"))
            {
                path = path[1..];
            }

            path = Path.Combine(AppContext.BaseDirectory, path);

            return StorageFile.GetFileFromPathAsync(path).AsTask();
        }
    }

    // Picked from: https://github.com/files-community/Files.
    public static async Task<string?> PickSingleFolderDialog(IntPtr windowHandle)
    {
        // WINUI3
        static FolderPicker InitializeWithWindow(FolderPicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);

            return obj;
        }

        var folderPicker = InitializeWithWindow(new FolderPicker(), windowHandle);
        folderPicker.FileTypeFilter.Add("*");

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder is not null)
        {
            return folder.Path;
        }
        return null;
    }
}
