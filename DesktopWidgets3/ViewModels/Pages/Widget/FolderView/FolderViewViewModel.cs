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

    private string FolderPath { get; set; } = string.Empty;

    private bool ShowIconOverlay { get; set; } = true;

    private bool ShowHiddenFile { get; set; } = false;

    #endregion

    #region current paths

    private readonly Stack<string> navigationFolderPaths = new();

    private string curFolderPath = string.Empty;
    private string? curParentFolderPath;
    private readonly FileSystemWatcher fileSystemWatcher = new();
    private string CurFolderPath
    {
        get => curFolderPath;
        set
        {
            if (curFolderPath != value)
            {
                curFolderPath = value;
                curParentFolderPath = Path.GetDirectoryName(CurFolderPath);
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Path = value;
                fileSystemWatcher.EnableRaisingEvents = true;
            }
        }
    }

    #endregion

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

    private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
    {
        var path = e.FullPath;
        if (!ShowHiddenFile && NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden))
        {
            return;
        }

        var isDirectory = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Directory);
        var directories = GetDirectories();
        if (isDirectory)
        {
            var index = directories.ToList().IndexOf(path);
            RunOnDispatcherQueue(async () => {
                var (fileIcon, fileIconOverlay) = await GetIcon(path, true);
                var item = new FolderViewFileItem()
                {
                    FileName = Path.GetFileName(path),
                    FilePath = path,
                    FileType = FileType.Folder,
                    Icon = fileIcon,
                    IconOverlay = fileIconOverlay,
                };
                FolderViewFileItems.Insert(index, item);
                await RefreshFolderIcon();
            });
        }
        else
        {
            var files = GetFiles();
            var index = directories.Length + files.ToList().IndexOf(path);
            RunOnDispatcherQueue(async () => {
                var (fileIcon, fileIconOverlay) = await GetIcon(path, false);
                var item = new FolderViewFileItem()
                {
                    FileName = Path.GetFileName(path),
                    FilePath = path,
                    FileType = FileType.File,
                    Icon = fileIcon,
                    IconOverlay = fileIconOverlay,
                };
                FolderViewFileItems.Insert(index, item);
                await RefreshFolderIcon();
            });
        }
    }

    private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        var path = e.FullPath;
        if (!ShowHiddenFile && NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden))
        {
            return;
        }

        var item = FolderViewFileItems.FirstOrDefault(item => item.FilePath == path);
        if (item != null)
        {
            var index = FolderViewFileItems.IndexOf(item);
            RunOnDispatcherQueue(async () => {
                FolderViewFileItems.RemoveAt(index);
                await RefreshFolderIcon();
            });
        }
    }

    private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
    {
        var oldPath = e.OldFullPath;
        var path = e.FullPath;
        if (!ShowHiddenFile && NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden))
        {
            return;
        }

        var item = FolderViewFileItems.FirstOrDefault(item => item.FilePath == oldPath);
        if (item != null)
        {
            item.FileName = Path.GetFileName(path);
            item.FilePath = path;

            var oldIndex = FolderViewFileItems.IndexOf(item);

            var isDirectory = item.FileType == FileType.Folder;
            var directories = GetDirectories();
            if (isDirectory)
            {
                var index = directories.ToList().IndexOf(path);
                RunOnDispatcherQueue(async () =>
                {
                    FolderViewFileItems.RemoveAt(oldIndex);
                    FolderViewFileItems.Insert(index, item);
                    await RefreshFolderIcon();
                });
            }
            else
            {
                var files = GetFiles();
                var index = directories.Length + files.ToList().IndexOf(path);
                RunOnDispatcherQueue(async () =>
                {
                    FolderViewFileItems.RemoveAt(oldIndex);
                    FolderViewFileItems.Insert(index, item);
                    await RefreshFolderIcon();
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
            if (Directory.Exists(path))
            {
                if (AllowNavigation)
                {
                    CurFolderPath = path;
                    await RefreshFileList(true);
                }
                else
                {
                    OpenHelper.OpenFolder(path);
                }
            }
            else
            {
                await RefreshFileList(false);
            }
        }
        else
        {
            if (File.Exists(path))
            {
                await OpenHelper.OpenFile(path, CurFolderPath);
            }
            else
            {
                await RefreshFileList(false);
            }
        }
    }

    private async void NavigateBack()
    {
        if (IsNavigateBackExecutable)
        {
            navigationFolderPaths.Pop();
            if (navigationFolderPaths.Count == 0)
            {
                CurFolderPath = FolderPath;
            }
            else
            {
                CurFolderPath = navigationFolderPaths.Peek();
            }
            await RefreshFileList(false);
        }
    }

    private async void NavigateUp()
    {
        if (IsNavigateUpExecutable)
        {
            CurFolderPath = curParentFolderPath!;
            await RefreshFileList(true);
        }
    }

    private async void NavigateRefresh()
    {
        await RefreshFileList(false);
    }

    internal void ToolbarDoubleTapped()
    {
        OpenHelper.OpenFolder(CurFolderPath);
    }

    #endregion

    #region refresh & icon

    [GeneratedRegex("^[a-zA-Z]:\\\\$")]
    private static partial Regex DiskRegex();

    private async Task RefreshFileList(bool pushFolderPath)
    {
        if (DiskRegex().IsMatch(CurFolderPath))
        {
            FolderName = CurFolderPath[..1];
        }
        else
        {
            FolderName = Path.GetFileName(CurFolderPath);
        }

        await RefreshFolderIcon();

        if (pushFolderPath)
        {
            navigationFolderPaths.Push(CurFolderPath);
            IsNavigateBackExecutable = navigationFolderPaths.Count > 0;
        }
        IsNavigateUpExecutable = curParentFolderPath != null;

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

    private async Task RefreshFolderIcon()
    {
        (FolderPathIcon, FolderPathIconOverlay) = await GetIcon(CurFolderPath, true);
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
        var directories = Directory.GetDirectories(CurFolderPath);
        if (!ShowHiddenFile)
        {
            directories = directories.Where(directory => !NativeFileOperationsHelper.HasFileAttribute(directory, FileAttributes.Hidden)).ToArray();
        }
        return directories;
    }

    private string[] GetFiles()
    {
        var files = Directory.GetFiles(CurFolderPath);
        if (!ShowHiddenFile)
        {
            files = files.Where(file => !NativeFileOperationsHelper.HasFileAttribute(file, FileAttributes.Hidden)).ToArray();
        }
        return files;
    }

    #endregion

    #region abstract methods

    protected async override void LoadSettings(FolderViewWidgetSettings settings)
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
            CurFolderPath = FolderPath;
            navigationFolderPaths.Clear();
            await RefreshFileList(false);
            needRefresh = false;
        }

        if (AllowNavigation != settings.AllowNavigation)
        {
            AllowNavigation = settings.AllowNavigation;
        }

        if (needRefresh)
        {
            await RefreshFileList(false);
        }
    }

    protected override FolderViewWidgetSettings GetSettings()
    {
        return new FolderViewWidgetSettings()
        {
            ShowIconOverlay = ShowIconOverlay,
            ShowHiddenFile = ShowHiddenFile,
            FolderPath = CurFolderPath,
            AllowNavigation = AllowNavigation,
        };
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            await RefreshFileList(false);
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
