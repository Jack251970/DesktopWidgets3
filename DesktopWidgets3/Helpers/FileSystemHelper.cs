using System.Diagnostics;
using Files.App.Helpers;
using Files.Shared.Helpers;

namespace DesktopWidgets3.Helpers;

public class FileSystemHelper
{
    public static async Task OpenFile(string[] path, string workingDirectory) => await OpenFile(path, workingDirectory, string.Empty);

    public static async Task OpenFile(string[] path, string workingDirectory, string args)
    {
        foreach (var item in path)
        {
            await OpenFile(item, workingDirectory, args);
        }
    }

    public static async Task OpenFile(string path, string workingDirectory) => await OpenFile(path, workingDirectory, string.Empty);

    public static async Task OpenFile(string path, string workingDirectory, string args)
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

    // Undone
    public static void OpenFolder(string path) => OpenFolder(path, string.Empty);

    public static void OpenFolder(string path, string args)
    {
        var process = new Process();
        process.StartInfo.FileName = "explorer.exe";
        process.StartInfo.Arguments = args + " \"" + path + "\"";
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.Verb = "open";
        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        process.Start();
    }
}
