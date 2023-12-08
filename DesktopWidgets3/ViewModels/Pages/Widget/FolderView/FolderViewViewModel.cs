using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Models.Widget.FolderView;
using DesktopWidgets3.ViewModels.Commands;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.Core.Data.Items;
using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.ViewModels.Pages.Widget.FolderView;

public partial class FolderViewViewModel : BaseWidgetViewModel, INavigationAware
{
    #region commands

    public ClickCommand NavigateBackCommand
    {
        get;
    }

    public ClickCommand NavigateUpCommand
    {
        get;
    }

    public ClickCommand NavigateRefreshCommand
    {
        get;
    }

    #endregion

    #region observable properties

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

    #endregion

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
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Path = value;
                fileSystemWatcher.EnableRaisingEvents = true;
            }
        }
    }

    private bool LoadIconOverlay { get; set; } = true;

    private readonly DispatcherQueue _dispatcherQueue = App.MainWindow!.DispatcherQueue;

    private bool _isInitialized;

    public FolderViewViewModel()
    {
        InitializeFileSystemWatcher();

        NavigateBackCommand = new ClickCommand(NavigateBack);
        NavigateUpCommand = new ClickCommand(NavigateUp);
        NavigateRefreshCommand = new ClickCommand(NavigateRefresh);
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is FolderViewWidgetSettings settings)
        {
            if (FolderPath != settings.FolderPath)
            {
                FolderPath = settings.FolderPath;
                await RefreshFileList(true, null, null);
            }

            if (LoadIconOverlay != settings.ShowIconOverlay)
            {
                LoadIconOverlay = settings.ShowIconOverlay;
                await RefreshFileList(false, FolderPathIcon, FolderPathIconOverlay);
            }

            _isInitialized = true;

            return;
        }
        
        if (!_isInitialized)
        {
            FolderPath = $"C:\\";
            await RefreshFileList(true, null, null);
            _isInitialized = true;
        }
    }

    public void OnNavigatedFrom()
    {

    }

    #region file system watcher

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

    #endregion

    #region command events

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
            await RefreshFileList(true, folderIcon, folderIconOverlay);
        }
        else
        {
            if (!File.Exists(filePath))
            {
                await RefreshFileList(false, FolderPathIcon, FolderPathIconOverlay);
            }
            else
            {
                await OpenFileHelper.OpenPath(filePath, string.Empty, FolderPath);
            }
        }
    }

    private async void NavigateBack()
    {
        if (IsNavigateBackExecutable)
        {
            navigationFolderPaths.Pop();
            FolderPath = navigationFolderPaths.Peek();
            await RefreshFileList(false, null, null);
        }
    }

    private async void NavigateUp()
    {
        if (IsNavigateUpExecutable)
        {
            FolderPath = parentFolderPath!;
            await RefreshFileList(true, null, null);
        }
    }

    private async void NavigateRefresh()
    {
        await RefreshFileList(false, FolderPathIcon, FolderPathIconOverlay);
    }

    #endregion

    #region refresh & icon

    private async Task RefreshFileList(bool pushFolderPath, BitmapImage? icon, BitmapImage? overlay)
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

    #endregion

    public async override void SetEditMode(bool editMode)
    {
        if (editMode)
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }
        else
        {
            await RefreshFileList(false, null, null);
            fileSystemWatcher.EnableRaisingEvents = true;
        }
    }
}
