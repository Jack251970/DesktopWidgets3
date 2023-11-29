using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.Core.Data.Items;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.ViewModels.WidgetsPages.Folder;

public partial class FolderViewViewModel : ObservableRecipient
{
    private readonly Stack<string> navigationFolderPaths = new();

    private string folderPath = $"C:\\Users\\11602\\OneDrive\\文档\\My-Data";
    private string? parentFolderPath;

    [ObservableProperty]
    private string _FolderName = string.Empty;

    [ObservableProperty]
    private bool _isNavigateBackExecutable = false;

    [ObservableProperty]
    private bool _isNavigateUpExecutable = false;

    [ObservableProperty]
    private BitmapImage? _folderPathIcon = null;

    public ObservableCollection<FolderViewFileItem> FolderViewFileItems { get; set; } = new();

    public FolderViewViewModel()
    {
        _ = LoadFileItemsFromFolderPath(true, null);
    }

    internal async Task FolderViewItemDoubleTapped(string filePath)
    {
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
            BitmapImage? folderPathIcon = null;
            foreach (var item in FolderViewFileItems)
            {
                if (item.FilePath == filePath)
                {
                    folderPathIcon = item.FileIcon;
                    break;
                }
            }
            await LoadFileItemsFromFolderPath(true, folderPathIcon);
        }
        else
        {
            if (!File.Exists(filePath))
            {
                await LoadFileItemsFromFolderPath(false, FolderPathIcon);
            }
            else
            {
                await OpenFileHelper.OpenPath(filePath, string.Empty, folderPath);
            }
        }
    }

    internal async Task NavigateBackButtonClick()
    {
        if (IsNavigateBackExecutable)
        {
            navigationFolderPaths.Pop();
            folderPath = navigationFolderPaths.Peek();
            await LoadFileItemsFromFolderPath(false, null);
        }
    }

    internal async Task NavigateUpButtonClick()
    {
        if (IsNavigateUpExecutable)
        {
            folderPath = parentFolderPath!;
            await LoadFileItemsFromFolderPath(true, null);
        }
    }

    private async Task LoadFileItemsFromFolderPath(bool pushFolderPath, BitmapImage? icon)
    {
        FolderName = Path.GetFileName(folderPath);
        if (icon is null)
        {
            (icon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(folderPath, true);
        }
        FolderPathIcon = icon;

        if (pushFolderPath)
        {
            navigationFolderPaths.Push(folderPath);
        }
        IsNavigateBackExecutable = navigationFolderPaths.Count > 1;
        parentFolderPath = Path.GetDirectoryName(folderPath);
        IsNavigateUpExecutable = parentFolderPath != null;

        FolderViewFileItems.Clear();

        foreach (var directory in Directory.GetDirectories(folderPath))
        {
            var directoryPath = directory;
            var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(directoryPath, FileAttributes.Hidden);
            if (!isHiddenItem)
            {
                var folderName = Path.GetFileName(directory);
                var (fileIcon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(directoryPath, true);
                FolderViewFileItems.Add(new FolderViewFileItem()
                {
                    FileName = folderName,
                    FilePath = directoryPath,
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
                var (fileIcon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(filePath, false);
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
