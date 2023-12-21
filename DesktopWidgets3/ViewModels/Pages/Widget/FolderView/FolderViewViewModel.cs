using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Models.Widget.FolderView;
using DesktopWidgets3.ViewModels.Commands;
using Files.App;
using Files.App.Data.Commands;
using Files.App.Data.Models;
using Files.App.Helpers;
using Files.App.Utils;
using Files.App.Utils.Storage;
using Files.App.Utils.Storage.Helpers;
using Files.App.ViewModels.Layouts;
using Files.Core.Data.Items;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using FileAttributes = System.IO.FileAttributes;

namespace DesktopWidgets3.ViewModels.Pages.Widget;

public partial class FolderViewViewModel : BaseWidgetViewModel<FolderViewWidgetSettings>, IWidgetUpdate, IWidgetClose, INotifyPropertyChanged
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

    #region current paths

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
            }
        }
    }

    #endregion

    #region select items

    public new event PropertyChangedEventHandler? PropertyChanged;

    public bool HasSelection => SelectedItems.Count != 0;

    private bool isItemSelected = false;
    public bool IsItemSelected
    {
        get => isItemSelected;
        internal set
        {
            if (value != isItemSelected)
            {
                isItemSelected = value;

                NotifyPropertyChanged(nameof(IsItemSelected));
            }
        }
    }

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
                    //SelectedItem = null;
                    SelectedItemsPropertiesViewModel.IsItemSelected = false;

                    /*ResetRenameDoubleClick();
                    UpdateSelectionSize();*/
                }
                else if (selectedItems is not null)
                {
                    IsItemSelected = true;
                    //SelectedItem = selectedItems.First();
                    SelectedItemsPropertiesViewModel.IsItemSelected = true;

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

                NotifyPropertyChanged(nameof(SelectedItems));
            }

            // ParentShellPageInstance!.ToolbarViewModel.SelectedItems = value;
        }
    }

    // This method is called by the Set accessor of each property.  
    // The CallerMemberName attribute that is applied to the optional propertyName  
    // parameter causes the property name of the caller to be substituted as an argument.  
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region some models (will be removed)

    public CurrentInstanceViewModel? InstanceViewModel = new();

    public BaseLayoutViewModel? CommandsViewModel = new();

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
            IsNavigateBackExecutable = navigationFolderPaths.Count > 0;
        }
        IsNavigateUpExecutable = curParentFolderPath != null;

        ListedItems.Clear();

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
        }

        ResetItemOpacity();
    }

    public virtual void ResetItemOpacity()
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
            return await FileIconHelper.GetFileIconAndOverlayAsync(filePath, isFolder);
        }
        else
        {
            return (await FileIconHelper.GetFileIconWithoutOverlayAsync(filePath, isFolder), null);
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

    #region Files.App.Data.Models.ItemViewModel

    public string WorkingDirectory { get; private set; } = null!;

    public Task<FilesystemResult<BaseStorageFile>> GetFileFromPathAsync(string path)
    {
        var paranetFolder = new StorageFolderWithPath(null!, CurFolderPath);
        return FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path, null!, paranetFolder));
    }

    public Task<FilesystemResult<BaseStorageFolder>> GetFolderFromPathAsync(string path)
    {
        var paranetFolder = new StorageFolderWithPath(null!, CurFolderPath);
        return FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, null!, paranetFolder));
    }
    
    #endregion
}
