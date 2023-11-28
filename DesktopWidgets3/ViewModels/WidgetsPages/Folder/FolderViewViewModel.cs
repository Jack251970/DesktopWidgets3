using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.ViewModels.WidgetsPages.Folder;

public partial class FolderViewViewModel : ObservableRecipient
{
    private static string folderPath = $"C:\\Users\\11602\\OneDrive\\文档\\My-Data";

    [ObservableProperty]
    private string _FolderName = string.Empty;

    public ObservableCollection<FolderViewFileItem> FolderViewFileItems { get; set; } = new();

    public FolderViewViewModel()
    {
        LoadFileItemsFromFolderPath();
    }

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
                await LaunchHelper.OpenPath(filePath, string.Empty, folderPath);
            }
        }
    }

    private async void LoadFileItemsFromFolderPath()
    {
        FolderName = Path.GetFileName(folderPath);

        FolderViewFileItems.Clear();

        foreach (var directory in Directory.GetDirectories(folderPath))
        {
            var folderPath = directory;
            var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(folderPath, FileAttributes.Hidden);
            if (!isHiddenItem)
            {
                var folderName = Path.GetFileName(directory);
                var (fileIcon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(folderPath, true);
                FolderViewFileItems.Add(new FolderViewFileItem()
                {
                    FileName = folderName,
                    FilePath = folderPath,
                    FileIcon = fileIcon,
                });
            }
        }

        foreach (var file in Directory.GetFiles(folderPath))
        {
            var filePath = file;
            var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden);
            if (!isHiddenItem)
            {
                var fileName = Path.GetFileName(file);
                var (fileIcon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(folderPath, false);
                FolderViewFileItems.Add(new FolderViewFileItem()
                {
                    FileName = fileName,
                    FilePath = filePath,
                    FileIcon = fileIcon,
                });
            }
        }
    }
}
