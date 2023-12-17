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
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.ViewModels.Pages.Widget.FolderView;

public partial class FolderViewViewModel : BaseWidgetViewModel<FolderViewWidgetSettings>, IWidgetUpdate, IWidgetClose
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

    [ObservableProperty]
    private bool _allowNavigation = true;

    #endregion

    #region settings

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

    private bool ShowIconOverlay { get; set; } = true;

    private bool ShowHiddenFile { get; set; } = false;

    #endregion

    private readonly Stack<string> navigationFolderPaths = new();

    public FolderViewViewModel()
    {
        InitializeFileSystemWatcher();

        NavigateBackCommand = new ClickCommand(NavigateBack);
        NavigateUpCommand = new ClickCommand(NavigateUp);
        NavigateRefreshCommand = new ClickCommand(NavigateRefresh);
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

    private async void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
    {
        var filePath = e.FullPath;
        if (!ShowHiddenFile && NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden))
        {
            return;
        }

        var isDirectory = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Directory);
        var (icon, iconOverlay) = await GetIcon(filePath, isDirectory);
        var item = new FolderViewFileItem()
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            FileType = isDirectory ? FileType.Folder : FileType.File,
            Icon = icon,
            IconOverlay = iconOverlay,
        };

        var directories = GetDirectories();
        if (isDirectory)
        {
            var index = directories.ToList().IndexOf(filePath);
            RunOnDispatcherQueue(() => FolderViewFileItems.Insert(index, item));
        }
        else
        {
            var files = GetFiles();
            var index = directories.Length + files.ToList().IndexOf(filePath);
            RunOnDispatcherQueue(() => FolderViewFileItems.Insert(index, item));
        }
    }

    private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        var filePath = e.FullPath;
        if (!ShowHiddenFile && NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden))
        {
            return;
        }

        var item = FolderViewFileItems.FirstOrDefault(item => item.FilePath == filePath);
        if (item != null)
        {
            var index = FolderViewFileItems.IndexOf(item);
            RunOnDispatcherQueue(() => FolderViewFileItems.RemoveAt(index));
        }
    }

    private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
    {
        var oldFilePath = e.OldFullPath;
        var filePath = e.FullPath;
        if (!ShowHiddenFile && NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden))
        {
            return;
        }

        var item = FolderViewFileItems.FirstOrDefault(item => item.FilePath == oldFilePath);
        if (item != null)
        {
            item.FileName = Path.GetFileName(filePath);
            item.FilePath = filePath;

            var oldIndex = FolderViewFileItems.IndexOf(item);

            var isDirectory = item.FileType == FileType.Folder;
            var directories = GetDirectories();
            if (isDirectory)
            {
                var index = directories.ToList().IndexOf(filePath);
                RunOnDispatcherQueue(() =>
                {
                    FolderViewFileItems.RemoveAt(oldIndex);
                    FolderViewFileItems.Insert(index, item);
                });
            }
            else
            {
                var files = GetFiles();
                var index = directories.Length + files.ToList().IndexOf(filePath);
                RunOnDispatcherQueue(() =>
                {
                    FolderViewFileItems.RemoveAt(oldIndex);
                    FolderViewFileItems.Insert(index, item);
                });
            }
        }
    }

    #endregion

    #region command events

    internal async Task FolderViewItemDoubleTapped(string path)
    {
        var isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(path);
        if (isShortcut)
        {
            var shortcutInfo = new ShellLinkItem();
            var shInfo = await FileOperationsHelpers.ParseLinkAsync(path);
            if (shInfo is null || shInfo.TargetPath is null || shortcutInfo.InvalidTarget)
            {
                return;
            }

            path = shInfo.TargetPath;
        }

        var isDirectory = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Directory);
        if (isDirectory)
        {
            if (AllowNavigation)
            {
                FolderPath = path;
                BitmapImage? folderIcon = null;
                BitmapImage? folderIconOverlay = null;
                foreach (var item in FolderViewFileItems)
                {
                    if (item.FilePath == path)
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
                OpenHelper.OpenFolder(path);
            }
        }
        else
        {
            if (!File.Exists(path))
            {
                await RefreshFileList(false, FolderPathIcon, FolderPathIconOverlay);
            }
            else
            {
                await OpenHelper.OpenFile(path, FolderPath);
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

    internal void ToolbarDoubleTapped()
    {
        OpenHelper.OpenFolder(FolderPath);
    }

    #endregion

    #region refresh & icon

    [GeneratedRegex("^[a-zA-Z]:\\\\$")]
    private static partial Regex DiskRegex();

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

        if (icon is null)
        {
            (icon, overlay) = await GetIcon(FolderPath, true);
        }
        FolderPathIcon = icon;
        FolderPathIconOverlay = overlay;

        if (pushFolderPath)
        {
            navigationFolderPaths.Push(FolderPath);
        }
        IsNavigateBackExecutable = navigationFolderPaths.Count > 1;
        IsNavigateUpExecutable = parentFolderPath != null;

        FolderViewFileItems.Clear();

        var directories = GetDirectories();
        foreach (var directory in directories)
        {
            var directoryPath = directory;
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

        var files = GetFiles();
        foreach (var file in files)
        {
            var filePath = file;
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

    private async Task<(BitmapImage? Icon, BitmapImage? Overlay)> GetIcon(string filePath, bool isFolder)
    {
        if (ShowIconOverlay)
        {
            return await FileIconHelper.GetFileIconAndOverlayAsync(filePath, isFolder);
        }
        else
        {
            return (await FileIconHelper.GetFileIconWithoutOverlayAsync(filePath, isFolder), null);
        }
    }

    private string[] GetDirectories()
    {
        var directories = Directory.GetDirectories(FolderPath);
        if (!ShowHiddenFile)
        {
            directories = directories.Where(directory => !NativeFileOperationsHelper.HasFileAttribute(directory, FileAttributes.Hidden)).ToArray();
        }
        return directories;
    }

    private string[] GetFiles()
    {
        var files = Directory.GetFiles(FolderPath);
        if (!ShowHiddenFile)
        {
            files = files.Where(file => !NativeFileOperationsHelper.HasFileAttribute(file, FileAttributes.Hidden)).ToArray();
        }
        return files;
    }

    #endregion

    #region abstract methods

    protected async override void LoadWidgetSettings(FolderViewWidgetSettings settings)
    {
        var needRefresh = false;

        if (ShowIconOverlay != settings.ShowIconOverlay)
        {
            ShowIconOverlay = settings.ShowIconOverlay;
            needRefresh = true;
        }

        if (ShowHiddenFile != settings.ShowHiddenFile)
        {
            ShowHiddenFile = settings.ShowHiddenFile;
            needRefresh = true;
        }

        if (FolderPath != settings.FolderPath)
        {
            FolderPath = settings.FolderPath;
            await RefreshFileList(false, null, null);
            needRefresh = false;
        }

        if (AllowNavigation != settings.AllowNavigation)
        {
            AllowNavigation = settings.AllowNavigation;
        }

        if (needRefresh)
        {
            await RefreshFileList(false, FolderPathIcon, FolderPathIconOverlay);
        }
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            await RefreshFileList(false, null, null);
            fileSystemWatcher.EnableRaisingEvents = true;
        }
        else
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }
    }

    public void WidgetWindow_Closing()
    {
        fileSystemWatcher.Dispose();
    }

    #endregion
}
