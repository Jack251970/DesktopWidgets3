﻿using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Models.Widget.FolderView;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.Core.Data.Items;
using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.ViewModels.Pages.Widget.FolderView;

public partial class FolderViewViewModel : ObservableRecipient, INavigationAware
{
    public ObservableCollection<FolderViewFileItem> FolderViewFileItems { get; set; } = new();

    [ObservableProperty]
    private string _FolderName = string.Empty;

    [ObservableProperty]
    private bool _isNavigateBackExecutable = false;

    [ObservableProperty]
    private bool _isNavigateUpExecutable = false;

    [ObservableProperty]
    private BitmapImage? _folderPathIcon = null;

    [ObservableProperty]
    private BitmapImage? _folderPathIconOverlay = null;

    private readonly Stack<string> navigationFolderPaths = new();

    private string folderPath = string.Empty;
    private string? parentFolderPath;
    private readonly FileSystemWatcher fileSystemWatcher = new();
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

    private bool loadIconOverlay = false;
    private bool LoadIconOverlay
    {
        get => loadIconOverlay;
        set
        {
            if (loadIconOverlay != value)
            {
                loadIconOverlay = value;
                _ = LoadFileItemsFromFolderPath(false, FolderPathIcon, FolderPathIconOverlay);
            }
        }
    }

    private readonly DispatcherQueue _dispatcherQueue = App.MainWindow!.DispatcherQueue;

    private bool _isInitialized;

    public FolderViewViewModel()
    {
        InitializeFileSystemWatcher();
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is FolderViewWidgetSettings settings)
        {
            FolderPath = settings.FolderPath;
            _ = LoadFileItemsFromFolderPath(true, null, null);
            _isInitialized = true;

            return;
        }
        
        if (!_isInitialized)
        {
            FolderPath = $"C:\\";
            _ = LoadFileItemsFromFolderPath(true, null, null);
            _isInitialized = true;
        }
    }

    public void OnNavigatedFrom()
    {

    }

    private void InitializeFileSystemWatcher()
    {
        fileSystemWatcher.IncludeSubdirectories = false;
        fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
        fileSystemWatcher.EnableRaisingEvents = false;

        fileSystemWatcher.Created += FileSystemWatcher_Created;
        fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
        fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;

        // TODO: Add changed event and NotifyFilters.LastWrite notify filter
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
            };

            var index = 0;
            var directories = Directory.GetDirectories(FolderPath);
            if (isDirectory)
            {
                foreach (var directory in directories)
                {
                    if (directory == filePath)
                    {
                        var (icon, iconOverlay) = await GetIcon(filePath, true);
                        item.Icon = icon;
                        item.IconOverlay = iconOverlay;
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
                    var (icon, iconOverlay) = await GetIcon(filePath, false);
                    item.Icon = icon;
                    item.IconOverlay = iconOverlay;
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
            BitmapImage? folderIcon = null;
            BitmapImage? folderIconOverlay = null;
            foreach (var item in FolderViewFileItems)
            {
                if (item.FilePath == filePath)
                {
                    folderIcon = item.Icon;
                    folderIconOverlay = item.IconOverlay;
                    break;
                }
            }
            await LoadFileItemsFromFolderPath(true, folderIcon, folderIconOverlay);
        }
        else
        {
            if (!File.Exists(filePath))
            {
                await LoadFileItemsFromFolderPath(false, FolderPathIcon, FolderPathIconOverlay);
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
            await LoadFileItemsFromFolderPath(false, null, null);
        }
    }

    internal async Task NavigateUpButtonClick()
    {
        if (IsNavigateUpExecutable)
        {
            FolderPath = parentFolderPath!;
            await LoadFileItemsFromFolderPath(true, null, null);
        }
    }

    internal async Task NavigateRefreshButtonClick()
    {
        await LoadFileItemsFromFolderPath(false, FolderPathIcon, FolderPathIconOverlay);
    }

    private async Task LoadFileItemsFromFolderPath(bool pushFolderPath, BitmapImage? icon, BitmapImage? overlay)
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
                var (fileIcon, fileIconOverlay) = await GetIcon(directoryPath, true);
                FolderViewFileItems.Add(new FolderViewFileItem()
                {
                    FileName = folderName,
                    FilePath = directoryPath,
                    FileType = FileType.Folder,
                    Icon = fileIcon,
                    IconOverlay = fileIconOverlay,
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
                var (fileIcon, fileIconOverlay) = await GetIcon(filePath, false);
                FolderViewFileItems.Add(new FolderViewFileItem()
                {
                    FileName = fileName,
                    FilePath = filePath,
                    FileType = FileType.File,
                    Icon = fileIcon,
                    IconOverlay = fileIconOverlay,
                });
            }
        }

        if (icon is null)
        {
            (icon, overlay) = await GetIcon(FolderPath, true);
        }
        FolderPathIcon = icon;
        FolderPathIconOverlay = overlay;
    }

    private async Task<(BitmapImage? Icon, BitmapImage? Overlay)> GetIcon(string filePath, bool isFolder)
    {
        if (LoadIconOverlay)
        {
            return await FileIconHelper.GetFileIconAndOverlayAsync(filePath, isFolder);
        }
        else
        {
            return (await FileIconHelper.GetFileIconWithoutOverlayAsync(filePath, isFolder), null);
        }
    }

    [GeneratedRegex("^[a-zA-Z]:\\\\$")]
    private static partial Regex DiskRegex();
}
