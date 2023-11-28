using Files.App.Helpers;
using Files.Shared.Helpers;

namespace DesktopWidgets3.Helpers;

public class OpenFileHelper
{
    public static async Task OpenPath(string path, string args, string workingDirectory)
    {
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden);
        var isScreenSaver = FileExtensionHelpers.IsScreenSaverFile(path);

        if (isHiddenItem)
        {
            // itemType = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory) ? FilesystemItemType.Directory : FilesystemItemType.File;
        }
        else
        {
            // TODO: 从网盘下载？
            // itemType = await StorageHelpers.GetTypeFromPath(path);
        }

        args ??= string.Empty;
        if (isScreenSaver)
        {
            args += "/s";
        }

        _ = await Win32Helpers.InvokeWin32ComponentAsync(path, args, workingDirectory);
    }
}
