using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.Core.Data.Items;
using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.ViewModels.Pages.Widget.FolderView;

public partial class FolderViewViewModel : ObservableRecipient, INavigationAware
{
    private readonly Stack<string> navigationFolderPaths = new();

    private string folderPath = string.Empty;
    private string FolderPath
    {
        get => folderPath;
        set
        {
            if (folderPath != value)
            {
                folderPath = value;
                parentFolderPath = Path.GetDirectoryName(FolderPath);
                fileSystemWatcher.Path = value;
                if (folderPath != string.Empty)
                {
                    fileSystemWatcher.EnableRaisingEvents = true;
                }
                else
                {
                    fileSystemWatcher.EnableRaisingEvents = false;
                }
            }
        }
    }
    private string? parentFolderPath;
    private readonly FileSystemWatcher fileSystemWatcher = new();

    [ObservableProperty]
    private string _FolderName = string.Empty;

    [ObservableProperty]
    private bool _isNavigateBackExecutable = false;

    [ObservableProperty]
    private bool _isNavigateUpExecutable = false;

    [ObservableProperty]
    private BitmapImage? _folderPathIcon = null;

    public ObservableCollection<FolderViewFileItem> FolderViewFileItems { get; set; } = new();

    private readonly DispatcherQueue _dispatcherQueue = App.MainWindow!.DispatcherQueue;

    public FolderViewViewModel()
    {
        fileSystemWatcher.Created += FileSystemWatcher_Created;
        fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
        fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
    }

    private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
    {
        var filePath = e.FullPath;
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden);
        if (isHiddenItem)
        {
            return;
        }

        var isDirectory = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Directory);
        _dispatcherQueue.TryEnqueue(async () => {
            var item = new FolderViewFileItem()
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileType = isDirectory ? FileType.Folder : FileType.File,
                FileIcon = null,
            };

            var index = 0;
            var directories = Directory.GetDirectories(FolderPath);
            if (isDirectory)
            {
                foreach (var directory in directories)
                {
                    if (directory == filePath)
                    {
                        var (fileIcon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(filePath, true);
                        item.FileIcon = fileIcon;
                        FolderViewFileItems.Insert(index, item);
                        return;
                    }
                    index++;
                }
            }

            index += directories.Length;
            var files = Directory.GetFiles(FolderPath);
            foreach (var file in files)
            {
                if (file == filePath)
                {
                    var (fileIcon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(filePath, false);
                    item.FileIcon = fileIcon;
                    FolderViewFileItems.Insert(index, item);
                    return;
                }
                index++;
            }
        });
    }

    private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        var filePath = e.FullPath;
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden);
        if (isHiddenItem)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() => {
            foreach (var item in FolderViewFileItems)
            {
                if (item.FilePath == filePath)
                {
                    FolderViewFileItems.Remove(item);
                    return;
                }
            }
        });
        
    }

    private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
    {
        var oldFilePath = e.OldFullPath;
        var filePath = e.FullPath;
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden);
        if (isHiddenItem)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            var index = 0;
            foreach (var item in FolderViewFileItems)
            {
                if (item.FilePath == oldFilePath)
                {
                    FolderViewFileItems.Remove(item);
                    item.FileName = Path.GetFileName(filePath);
                    item.FilePath = filePath;
                    FolderViewFileItems.Insert(index, item);
                    break;
                }
                index++;
            }
        });
    }

    public void OnNavigatedTo(object parameter)
    {
        FolderPath = $"C:\\Users\\11602\\OneDrive\\文档\\My-Data";
        _ = LoadFileItemsFromFolderPath(true, null);
    }

    public void OnNavigatedFrom()
    {
        
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
            FolderPath = filePath;
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
                await OpenFileHelper.OpenPath(filePath, string.Empty, FolderPath);
            }
        }
    }

    internal async Task NavigateBackButtonClick()
    {
        if (IsNavigateBackExecutable)
        {
            navigationFolderPaths.Pop();
            FolderPath = navigationFolderPaths.Peek();
            await LoadFileItemsFromFolderPath(false, null);
        }
    }

    internal async Task NavigateUpButtonClick()
    {
        if (IsNavigateUpExecutable)
        {
            FolderPath = parentFolderPath!;
            await LoadFileItemsFromFolderPath(true, null);
        }
    }

    internal async Task NavigateRefreshButtonClick()
    {
        await LoadFileItemsFromFolderPath(false, FolderPathIcon);
    }

    private async Task LoadFileItemsFromFolderPath(bool pushFolderPath, BitmapImage? icon)
    {
        if (DiskRegex().IsMatch(FolderPath))
        {
            FolderName = FolderPath[..1];
        }
        else
        {
            FolderName = Path.GetFileName(FolderPath);
        }

        if (pushFolderPath)
        {
            navigationFolderPaths.Push(FolderPath);
        }
        IsNavigateBackExecutable = navigationFolderPaths.Count > 1;
        IsNavigateUpExecutable = parentFolderPath != null;

        FolderViewFileItems.Clear();

        var directories = Directory.GetDirectories(FolderPath);
        foreach (var directory in directories)
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
                    FileType = FileType.Folder,
                    FileIcon = fileIcon,
                });
            }
        }

        var files = Directory.GetFiles(FolderPath);
        foreach (var file in files)
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
                    FileType = FileType.File,
                    FileIcon = fileIcon,
                });
            }
        }

        // TODO: fix icon loading bug
        if (icon is null)
        {
            (icon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(FolderPath, true);
        }
        FolderPathIcon = icon;
    }

    [GeneratedRegex("^[a-zA-Z]:\\\\$")]
    private static partial Regex DiskRegex();
}
