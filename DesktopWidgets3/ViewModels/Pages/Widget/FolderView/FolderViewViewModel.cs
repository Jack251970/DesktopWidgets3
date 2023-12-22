using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;
using Files.App.Utils;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Commands;
using Files.App;
using Files.App.Data.Commands;
using Files.App.Data.Models;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.App.Utils.Storage.Helpers;
using Files.App.ViewModels.Layouts;
using Files.Core.Data.Items;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Files.App.Utils.Cloud;
using Files.Core.Data.Enums;
using Files.App.Utils.Shell;
using System.Runtime.InteropServices;
using Files.Shared.Extensions;
using FileAttributes = System.IO.FileAttributes;
using static Files.Core.Helpers.NativeFindStorageItemHelper;

namespace DesktopWidgets3.ViewModels.Pages.Widget;

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

    #region view properties

    public ObservableCollection<ListedItem> ListedItems { get; set; } = new();

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

    #region current path

    private readonly Stack<string> navigationFolderPaths = new();

    private string curFolderPath = string.Empty;
    private string? curParentFolderPath;
    private readonly FileSystemWatcher fileSystemWatcher = new();
    public string CurFolderPath
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
                WorkingDirectory = value;
                currentStorageFolder = new StorageFolderWithPath(null!, value);
            }
        }
    }

    #endregion

    #region select items

    [ObservableProperty]
    public bool _hasSelection = false;

    private bool isItemSelected = false;
    public bool IsItemSelected
    {
        get => isItemSelected;
        internal set
        {
            if (value != isItemSelected)
            {
                isItemSelected = value;
            }
        }
    }

    public ListedItem? SelectedItem { get; private set; }

    private List<ListedItem> selectedItems = new();
    public List<ListedItem> SelectedItems
    {
        get => selectedItems;
        internal set
        {
            if (value != selectedItems)
            {
                selectedItems = value;

                if (selectedItems?.Count == 0 || selectedItems?[0] is null)
                {
                    IsItemSelected = false;
                    SelectedItem = null;
                    /*SelectedItemsPropertiesViewModel.IsItemSelected = false;*/

                    /*ResetRenameDoubleClick();
                    UpdateSelectionSize();*/
                }
                else if (selectedItems is not null)
                {
                    IsItemSelected = true;
                    SelectedItem = selectedItems.First();
                    /*SelectedItemsPropertiesViewModel.IsItemSelected = true;*/

                    /*UpdateSelectionSize();

                    SelectedItemsPropertiesViewModel.SelectedItemsCount = selectedItems.Count;

                    if (selectedItems.Count == 1)
                    {
                        SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{selectedItems.Count} {"ItemSelected/Text".GetLocalizedResource()}";
                        DispatcherQueue.EnqueueOrInvokeAsync(async () =>
                        {
                            // Tapped event must be executed first
                            await Task.Delay(50);
                            preRenamingItem = SelectedItem;
                        });
                    }
                    else
                    {
                        SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{selectedItems!.Count} {"ItemsSelected/Text".GetLocalizedResource()}";
                        ResetRenameDoubleClick();
                    }*/
                }

                HasSelection = SelectedItems.Count != 0;
            }

            // ParentShellPageInstance!.ToolbarViewModel.SelectedItems = value;
        }
    }

    #endregion

    #region rename items

    public bool IsRenamingItem { get; set; }

    public ListedItem? RenamingItem { get; set; }

    public string? OldItemName { get; set; }

    #endregion

    #region some models (maybe will be removed)

    public CurrentInstanceViewModel InstanceViewModel = new();

    public BaseLayoutViewModel CommandsViewModel = new();

    public ItemManipulationModel ItemManipulationModel = new();

    public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel = new();

    #endregion

    private readonly ICommandManager _commandManager;
    private readonly IFileSystemHelpers _fileSystemHelpers;

    public ICommandManager CommandManager => _commandManager;
    public IFileSystemHelpers FileSystemHelpers => _fileSystemHelpers;

    public FolderViewViewModel(ICommandManager commandManager, IFileSystemHelpers fileSystemHelpers)
    {
        _commandManager = commandManager;
        _commandManager.Initialize(this);
        _fileSystemHelpers = fileSystemHelpers;

        NavigateBackCommand = new ClickCommand(NavigateBack);
        NavigateUpCommand = new ClickCommand(NavigateUp);
        NavigateRefreshCommand = new ClickCommand(NavigateRefresh);

        InitializeFileSystemWatcher();
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
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden);
        if (!ShowHiddenFile && isHiddenItem)
        {
            return;
        }

        var isDirectory = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Directory);
        var directories = GetDirectories(!ShowHiddenFile);
        if (isDirectory)
        {
            var index = directories.ToList().IndexOf(path);
            RunOnDispatcherQueue(async () => {
                var fileExtension = Path.GetExtension(path);
                var (fileIcon, fileIconOverlay) = await GetIcon(path, true);
                var item = new ListedItem()
                {
                    ItemNameRaw = Path.GetFileName(path),
                    ItemPath = path,
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    FileImage = fileIcon,
                    IconOverlay = fileIconOverlay,
                    IsHiddenItem = isHiddenItem,
                    FileExtension = fileExtension,
                };
                ListedItems.Insert(index, item);
                await RefreshFolderIcon();
            });
        }
        else
        {
            var files = GetFiles(!ShowHiddenFile);
            var index = directories.Length + files.ToList().IndexOf(path);
            RunOnDispatcherQueue(async () => {
                var (fileIcon, fileIconOverlay) = await GetIcon(path, false);
                var item = new ListedItem()
                {
                    ItemNameRaw = Path.GetFileName(path),
                    ItemPath = path,
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileImage = fileIcon,
                    IconOverlay = fileIconOverlay,
                    IsHiddenItem = isHiddenItem,
                };
                ListedItems.Insert(index, item);
                await RefreshFolderIcon();
            });
        }
    }

    private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        var path = e.FullPath;
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden);
        if (!ShowHiddenFile && isHiddenItem)
        {
            return;
        }

        var item = ListedItems.FirstOrDefault(item => item.ItemPath == path);
        if (item != null)
        {
            var index = ListedItems.IndexOf(item);
            RunOnDispatcherQueue(async () => {
                ListedItems.RemoveAt(index);
                await RefreshFolderIcon();
            });
        }
    }

    private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
    {
        var oldPath = e.OldFullPath;
        var path = e.FullPath;
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden);
        if (!ShowHiddenFile && isHiddenItem)
        {
            return;
        }

        var item = ListedItems.FirstOrDefault(item => item.ItemPath == oldPath);
        if (item != null)
        {
            item.ItemNameRaw = Path.GetFileName(path);
            item.ItemPath = path;

            var oldIndex = ListedItems.IndexOf(item);

            var isDirectory = item.PrimaryItemAttribute == StorageItemTypes.Folder;
            var directories = GetDirectories(!ShowHiddenFile);
            if (isDirectory)
            {
                var index = directories.ToList().IndexOf(path);
                RunOnDispatcherQueue(async () =>
                {
                    ListedItems.RemoveAt(oldIndex);
                    ListedItems.Insert(index, item);
                    await RefreshFolderIcon();
                });
            }
            else
            {
                var files = GetFiles(!ShowHiddenFile);
                var index = directories.Length + files.ToList().IndexOf(path);
                RunOnDispatcherQueue(async () =>
                {
                    ListedItems.RemoveAt(oldIndex);
                    ListedItems.Insert(index, item);
                    await RefreshFolderIcon();
                });
            }
        }
    }

    #endregion

    #region command events

    internal async Task OpenItem(string path)
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
                    Helpers.FileSystemHelper.OpenFolder(path);
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
                await Helpers.FileSystemHelper.OpenFile(path, CurFolderPath);
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
        Helpers.FileSystemHelper.OpenFolder(CurFolderPath);
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
        }
        IsNavigateBackExecutable = navigationFolderPaths.Count > 0;
        IsNavigateUpExecutable = curParentFolderPath != null;

        /*ListedItems.Clear();

        var directories = GetDirectories(false);
        foreach (var directory in directories)
        {
            var directoryPath = directory;
            var folderName = Path.GetFileName(directory);
            var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(directory, FileAttributes.Hidden);
            if (!ShowHiddenFile && isHiddenItem)
            {
                continue;
            }

            var (fileIcon, fileIconOverlay) = await GetIcon(directoryPath, true);
            ListedItems.Add(new ListedItem()
            {
                ItemNameRaw = folderName,
                ItemPath = directoryPath,
                PrimaryItemAttribute = StorageItemTypes.Folder,
                FileImage = fileIcon,
                IconOverlay = fileIconOverlay,
                IsHiddenItem = isHiddenItem,
            });
        }

        var files = GetFiles(false);
        foreach (var file in files)
        {
            var filePath = file;
            var fileName = Path.GetFileName(file);
            var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden);
            if (!ShowHiddenFile && isHiddenItem)
            {
                continue;
            }

            var fileExtension = Path.GetExtension(filePath);
            var (fileIcon, fileIconOverlay) = await GetIcon(filePath, false);
            ListedItems.Add(new ListedItem()
            {
                ItemNameRaw = fileName,
                ItemPath = filePath,
                PrimaryItemAttribute = StorageItemTypes.File,
                FileImage = fileIcon,
                IconOverlay = fileIconOverlay,
                IsHiddenItem = isHiddenItem,
                FileExtension = fileExtension,
            });
        }*/

        await EnumerateItemsFromStandardFolderAsync(CurFolderPath);

        ResetItemOpacity();
    }

    private void ResetItemOpacity()
    {
        foreach (var item in ListedItems)
        {
            if (item is not null)
            {
                item.Opacity = item.IsHiddenItem ? Constants.UI.DimItemOpacity : 1.0d;
            }
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
            var iconInfo = await FileThumbnailHelper.LoadIconAndOverlayAsync(filePath, 96, isFolder);
            return (await iconInfo.IconData.ToBitmapAsync(), await iconInfo.OverlayData.ToBitmapAsync());
        }
        else
        {
            var iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(filePath, 96, isFolder);
            return (await iconData.ToBitmapAsync(), null);
        }
    }

    private string[] GetDirectories(bool removeHidden)
    {
        var directories = Directory.GetDirectories(CurFolderPath);
        if (removeHidden)
        {
            directories = directories.Where(directory => !NativeFileOperationsHelper.HasFileAttribute(directory, FileAttributes.Hidden)).ToArray();
        }
        return directories;
    }

    private string[] GetFiles(bool removeHidden)
    {
        var files = Directory.GetFiles(CurFolderPath);
        if (removeHidden)
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

    public async Task<bool> NavigateToPath(string path)
    {
        if (AllowNavigation)
        {
            CurFolderPath = path;
            await RefreshFileList(true);
            return true;
        }
        return false;
    }

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
        DefaultIcons.Clear();
    }

    #endregion

    #region Files.App.Data.Models.ItemViewModel

    private readonly string folderTypeTextLocalized = "Folder".GetLocalized();

    public ListedItem? CurrentFolder { get; private set; }
    public string WorkingDirectory { get; private set; } = null!;

    private StorageFolderWithPath? currentStorageFolder;
    private StorageFolderWithPath? workingRoot = null;

    // TODO: Use default icon to load items quickly.
    public Dictionary<string, BitmapImage> DefaultIcons = new();
    private uint currentDefaultIconSize = 0;

    public Task<FilesystemResult<BaseStorageFile>> GetFileFromPathAsync(string path) 
        => FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path, workingRoot!, currentStorageFolder!));

    public Task<FilesystemResult<BaseStorageFolder>> GetFolderFromPathAsync(string path) 
        => FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, workingRoot!, currentStorageFolder!));

    private async Task<int> EnumerateItemsFromStandardFolderAsync(string path, CancellationToken? cancellationToken = null, LibraryItem? library = null)
    {
        // Flag to use FindFirstFileExFromApp or StorageFolder enumeration - Use storage folder for Box Drive (#4629)
        var isBoxFolder = CloudDrivesManager.Drives.FirstOrDefault(x => x.Text == "Box")?.Path?.TrimEnd('\\') is string boxFolder && path.StartsWith(boxFolder);
        var isWslDistro = path.StartsWith(@"\\wsl$\", StringComparison.OrdinalIgnoreCase) || path.StartsWith(@"\\wsl.localhost\", StringComparison.OrdinalIgnoreCase)
            || path.Equals(@"\\wsl$", StringComparison.OrdinalIgnoreCase) || path.Equals(@"\\wsl.localhost", StringComparison.OrdinalIgnoreCase);
        var isMtp = path.StartsWith(@"\\?\", StringComparison.Ordinal);
        var isShellFolder = path.StartsWith(@"\\SHELL\", StringComparison.Ordinal);
        var isNetwork = path.StartsWith(@"\\", StringComparison.Ordinal) &&
            !isMtp &&
            !isShellFolder &&
            !isWslDistro;
        var isFtp = FtpHelpers.IsFtpPath(path);
        var enumFromStorageFolder = isBoxFolder || isFtp;

        BaseStorageFolder? rootFolder = null;

        if (isNetwork)
        {
            /*var auth = await NetworkDrivesAPI.AuthenticateNetworkShare(path);
            if (!auth)
            {
                return -1;
            }*/
            return -1;
        }

        if (!enumFromStorageFolder && FolderHelpers.CheckFolderAccessWithWin32(path))
        {
            // Will enumerate with FindFirstFileExFromApp, rootFolder only used for Bitlocker
            currentStorageFolder = null;
        }
        else if (workingRoot is not null)
        {
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path, workingRoot!, currentStorageFolder!));
            if (!res)
            {
                return -1;
            }

            currentStorageFolder = res.Result;
            rootFolder = currentStorageFolder.Item;
            enumFromStorageFolder = true;
        }
        else
        {
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path, workingRoot!, currentStorageFolder!));
            if (res)
            {
                currentStorageFolder = res.Result;
                rootFolder = currentStorageFolder.Item;
            }
            else if (res == FileSystemStatusCode.Unauthorized)
            {
                // TODO: Show dialog
                /*await DialogDisplayHelper.ShowDialogAsync(
                    "AccessDenied".GetLocalizedResource(),
                    "AccessDeniedToFolder".GetLocalizedResource());*/

                return -1;
            }
            else if (res == FileSystemStatusCode.NotFound)
            {
                // TODO: Show dialog
                /*await DialogDisplayHelper.ShowDialogAsync(
                    "FolderNotFoundDialog/Title".GetLocalizedResource(),
                    "FolderNotFoundDialog/Text".GetLocalizedResource());*/

                return -1;
            }
            else
            {
                // TODO: Show dialog
                /*await DialogDisplayHelper.ShowDialogAsync(
                    "DriveUnpluggedDialog/Title".GetLocalizedResource(),
                    res.ErrorCode.ToString());*/

                return -1;
            }
        }

        var pathRoot = Path.GetPathRoot(path);
        if (Path.IsPathRooted(path) && pathRoot == path)
        {
            rootFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
            if (await FolderHelpers.CheckBitlockerStatusAsync(rootFolder, WorkingDirectory))
            {
                await ContextMenu.InvokeVerb("unlock-bde", pathRoot);
            }
        }

        //HasNoWatcher = isFtp || isWslDistro || isMtp || currentStorageFolder?.Item is ZipStorageFolder;

        if (enumFromStorageFolder)
        {
            var basicProps = await rootFolder?.GetBasicPropertiesAsync();
            var currentFolder = library ?? new ListedItem()//rootFolder?.FolderRelativeId ?? string.Empty)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemPropertiesInitialized = true,
                ItemNameRaw = rootFolder?.DisplayName ?? string.Empty,
                ItemDateModifiedReal = basicProps.DateModified,
                ItemType = rootFolder?.DisplayType ?? string.Empty,
                FileImage = null,
                LoadFileIcon = false,
                ItemPath = string.IsNullOrEmpty(rootFolder?.Path) ? currentStorageFolder?.Path ?? string.Empty : rootFolder.Path,
                FileSize = null!,
                FileSizeBytes = 0,
            };

            if (library is null)
            {
                currentFolder.ItemDateCreatedReal = rootFolder?.DateCreated ?? DateTimeOffset.Now;
            }

            CurrentFolder = currentFolder;
            await EnumFromStorageFolderAsync(path, rootFolder, currentStorageFolder, cancellationToken);

            // Workaround for #7428
            return isBoxFolder ? 2 : 1;
        }
        else
        {
            var (hFile, findData, errorCode) = await Task.Run(() =>
            {
                var findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
                var additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

                var hFileTsk = FindFirstFileExFromApp(
                    path + "\\*.*",
                    findInfoLevel,
                    out var findDataTsk,
                    FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                    IntPtr.Zero,
                    additionalFlags);

                return (hFileTsk, findDataTsk, hFileTsk.ToInt64() == -1 ? Marshal.GetLastWin32Error() : 0);
            })
            .WithTimeoutAsync(TimeSpan.FromSeconds(5));

            var itemModifiedDate = DateTime.Now;
            var itemCreatedDate = DateTime.Now;

            try
            {
                FileTimeToSystemTime(ref findData.ftLastWriteTime, out var systemModifiedTimeOutput);
                itemModifiedDate = systemModifiedTimeOutput.ToDateTime();

                FileTimeToSystemTime(ref findData.ftCreationTime, out var systemCreatedTimeOutput);
                itemCreatedDate = systemCreatedTimeOutput.ToDateTime();
            }
            catch (ArgumentException)
            {
            }

            var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            var opacity = isHidden ? Constants.UI.DimItemOpacity : 1d;

            var currentFolder = library ?? new ListedItem()//null)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemPropertiesInitialized = true,
                ItemNameRaw = rootFolder?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\')),
                ItemDateModifiedReal = itemModifiedDate,
                ItemDateCreatedReal = itemCreatedDate,
                ItemType = folderTypeTextLocalized,
                FileImage = null,
                IsHiddenItem = isHidden,
                Opacity = opacity,
                LoadFileIcon = false,
                ItemPath = path,
                FileSize = null!,
                FileSizeBytes = 0,
            };

            CurrentFolder = currentFolder;

            if (hFile == IntPtr.Zero)
            {
                // TODO: Show dialog
                /*await DialogDisplayHelper.ShowDialogAsync("DriveUnpluggedDialog/Title".GetLocalizedResource(), "");*/
                return -1;
            }
            else if (hFile.ToInt64() == -1)
            {
                await EnumFromStorageFolderAsync(path, rootFolder, currentStorageFolder, cancellationToken);

                // TODO: Show dialog
                /*// errorCode == ERROR_ACCESS_DENIED
                if (filesAndFolders.Count == 0 && errorCode == 0x5)
                {
                    await DialogDisplayHelper.ShowDialogAsync(
                        "AccessDenied".GetLocalizedResource(),
                        "AccessDeniedToFolder".GetLocalizedResource());

                    return -1;
                }*/

                return 1;
            }
            else
            {
                await Task.Run(async () =>
                {
                    var fileList = await Win32StorageEnumerator.ListEntries(path, hFile, findData, cancellationToken, -1, intermediateAction: async (intermediateList) =>
                    {
                        AddItems(ListedItems, intermediateList);
                        //await OrderFilesAndFoldersAsync();
                        //await ApplyFilesAndFoldersChangesAsync();
                    }, defaultIconPairs: DefaultIcons, showHiddenFile:ShowHiddenFile);

                    AddItems(ListedItems, fileList);
                    //await OrderFilesAndFoldersAsync();
                    //await ApplyFilesAndFoldersChangesAsync();

                    //await dispatcherQueue.EnqueueOrInvokeAsync(CheckForSolutionFile, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
                });

                rootFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
                if (rootFolder?.DisplayName is not null)
                {
                    currentFolder.ItemNameRaw = rootFolder.DisplayName;
                }

                return 0;
            }
        }
    }

    private async Task EnumFromStorageFolderAsync(string path, BaseStorageFolder? rootFolder, StorageFolderWithPath currentStorageFolder, CancellationToken? cancellationToken)
    {
        if (rootFolder is null)
        {
            return;
        }

        if (rootFolder is IPasswordProtectedItem ppis)
        {
            // TODO: Request password
            //ppis.PasswordRequestedCallback = UIFilesystemHelpers.RequestPassword;
        }

        await Task.Run(async () =>
        {
            var finalList = await UniversalStorageEnumerator.ListEntries(rootFolder, currentStorageFolder, cancellationToken, -1,
                async (intermediateList) =>
                {
                    AddItems(ListedItems, intermediateList);
                    //await OrderFilesAndFoldersAsync();
                    //await ApplyFilesAndFoldersChangesAsync();
                },
                defaultIconPairs: DefaultIcons);

            AddItems(ListedItems, finalList);
            //await OrderFilesAndFoldersAsync();
            //await ApplyFilesAndFoldersChangesAsync();
        }, cancellationToken == null ? CancellationToken.None : (CancellationToken)cancellationToken);

        if (rootFolder is IPasswordProtectedItem ppiu)
        {
            ppiu.PasswordRequestedCallback = null!;
        }
    }

    /*private async Task GetDefaultItemIconsAsync(uint size)
    {
        if (currentDefaultIconSize == size)
        {
            return;
        }

        // TODO: Add more than just the folder icon
        DefaultIcons.Clear();

        using StorageItemThumbnail icon = await FilesystemTasks.Wrap(() => StorageItemIconHelpers.GetIconForItemType(size, IconPersistenceOptions.Persist));
        if (icon is not null)
        {
            var img = new BitmapImage();
            await img.SetSourceAsync(icon);
            DefaultIcons.Add(string.Empty, img);
        }

        currentDefaultIconSize = size;
    }*/

    private void AddItems<T>(ObservableCollection<T> list, List<T> items)
    {
        if (items.Any())
        {
            RunOnDispatcherQueue(() =>
            {
                list.Clear();
                foreach (var item in items)
                {
                    list.Add(item);
                }
            });
        }
    }

    #endregion
}
