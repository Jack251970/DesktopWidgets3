using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.ViewModels.WidgetsPages.Folder;

public partial class FolderViewViewModel : ObservableRecipient
{
    private static string folderPath = $"C:\\Users\\11602\\Downloads";

    [ObservableProperty]
    private string _FolderName = string.Empty;

    public ObservableCollection<FolderViewFileItem> FolderViewFileItems { get; set; } = new();

    public FolderViewViewModel()
    {
        LoadFileItemsFromFolderPath();
    }

    [Obsolete]
    internal async void FolderViewItemClick(object sender)
    {
        var button = sender as Button;
        if (button == null)
        {
            return;
        }

        if (button.Tag is not string filePath)
        {
            return;
        }
        var isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(filePath);
        if (isShortcut)
        {
            var shortcutInfo = new ShellLinkItem();
            var shInfo = await FileOperationsHelpers.ParseLinkAsync(filePath);
            if (shInfo is null || shInfo.TargetPath is null || shortcutInfo.InvalidTarget)
            {
                return;
            }

            filePath = shInfo.TargetPath;
        }

        var isDirectory = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Directory);
        if (isDirectory)
        {
            folderPath = filePath;
            LoadFileItemsFromFolderPath();
        }
        else
        {
            if (!File.Exists(filePath))
            {
                LoadFileItemsFromFolderPath();
            }
            else
            {
                await OpenPath(filePath);
            }
        }
    }

    private async void LoadFileItemsFromFolderPath()
    {
        FolderName = Path.GetFileName(folderPath);

        FolderViewFileItems.Clear();

        foreach (var directory in Directory.GetDirectories(folderPath))
        {
            var folderName = Path.GetFileName(directory);
            var folderPath = directory;
            var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(folderPath, FileAttributes.Hidden);
            if (!isHiddenItem)
            {
                FolderViewFileItems.Add(new FolderViewFileItem()
                {
                    FileName = folderName,
                    FilePath = folderPath,
                    FileIcon = await GetFileIcon(folderPath, true),
                });
            }
        }

        foreach (var file in Directory.GetFiles(folderPath))
        {
            var fileName = Path.GetFileName(file);
            var filePath = file;
            var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden);
            if (!isHiddenItem)
            {
                FolderViewFileItems.Add(new FolderViewFileItem()
                {
                    FileName = fileName,
                    FilePath = filePath,
                    FileIcon = await GetFileIcon(filePath, false),
                });
            }
        }
    }

    private static async Task<BitmapImage?> GetFileIcon(string filePath, bool isFolder)
    {
        var (iconData, _) = await FileIconHelper.LoadIconAndOverlayAsync(filePath, 96, isFolder);

        return await iconData.ToBitmapAsync();

        /*if (iconInfo.OverlayData is not null)
        {
            await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
            {
                item.IconOverlay = await iconInfo.OverlayData.ToBitmapAsync();
                item.ShieldIcon = await GetShieldIcon();
            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
        }*/
    }

    [Obsolete]
    public static async Task OpenPath(string path, string? args = default)
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

        _ = await LaunchHelper.LaunchAppAsync(path, args, folderPath);
    }
}
