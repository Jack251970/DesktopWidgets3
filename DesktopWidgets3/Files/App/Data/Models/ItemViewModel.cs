// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using Files.App.Helpers;
using Files.App.Utils;
using Files.App.Utils.Cloud;
using Files.App.Utils.Shell;
using Files.App.Utils.Storage;
using Files.App.Utils.Storage.Helpers;
using Files.Core.Data.Enums;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Files.Shared.Extensions;
using Files.App.Utils.Git;
using Files.Core.Utils.Cloud;
using System.Diagnostics;
using Windows.Storage.FileProperties;
using Files.App.Extensions;
using DesktopWidgets3.ViewModels.Pages.Widget;
using System.Collections.Concurrent;
using Microsoft.UI.Xaml.Media;
using Files.App.ViewModels.Previews;
using FileAttributes = System.IO.FileAttributes;
using static Files.Core.Helpers.NativeFindStorageItemHelper;
using static Vanara.PInvoke.Ole32;

namespace Files.App.Data.Models;

public sealed class ItemViewModel : ObservableObject, IDisposable
{
    #region properties

    private readonly SemaphoreSlim enumFolderSemaphore;
    private readonly ConcurrentDictionary<string, bool> itemLoadQueue;
    private readonly DispatcherQueue dispatcherQueue;
    private readonly IStorageCacheController fileListCache = StorageCacheController.GetInstance();
    private readonly string folderTypeTextLocalized = "Folder".GetLocalized();

    private Task? aProcessQueueAction;
    private Task? gitProcessQueueAction;

    // Files and folders list for manipulating
    private ConcurrentCollection<ListedItem> filesAndFolders;

    private readonly LayoutPreferencesManager folderSettings;

    // Only used for Binding and ApplyFilesAndFoldersChangesAsync, don't manipulate on this!
    public BulkConcurrentObservableCollection<ListedItem> FilesAndFolders { get; }

    private ListedItem? currentFolder;
    public ListedItem? CurrentFolder
    {
        get => currentFolder;
        private set => SetProperty(ref currentFolder, value);
    }

    public delegate void WorkingDirectoryModifiedEventHandler(object sender, WorkingDirectoryModifiedEventArgs e);
    public event WorkingDirectoryModifiedEventHandler? WorkingDirectoryModified;

    public delegate void PageTypeUpdatedEventHandler(object sender, PageTypeUpdatedEventArgs e);
    public event PageTypeUpdatedEventHandler? PageTypeUpdated;

    public delegate void ItemLoadStatusChangedEventHandler(object sender, ItemLoadStatusChangedEventArgs e);
    public event ItemLoadStatusChangedEventHandler? ItemLoadStatusChanged;

    public string WorkingDirectory { get; private set; } = string.Empty;

    public string? GitDirectory { get; private set; }

    public bool IsValidGitDirectory { get; private set; }

    private StorageFolderWithPath? currentStorageFolder;
    private StorageFolderWithPath workingRoot = null!;

    private FileSystemWatcher watcher = null!;

    private static BitmapImage shieldIcon = null!;

    public event EventHandler? DirectoryInfoUpdated;

    public bool HasNoWatcher { get; private set; }

    private bool isLoadingItems = false;
    public bool IsLoadingItems
    {
        get => isLoadingItems;
        set => isLoadingItems = value;
    }

    private bool IsLoadingCancelled { get; set; }

    private CancellationTokenSource addFilesCTS;
    private CancellationTokenSource semaphoreCTS;
    private CancellationTokenSource loadPropsCTS;
    private CancellationTokenSource watcherCTS;

    private bool IsSearchResults { get; set; }

    private readonly FolderViewViewModel ViewModel;

    #endregion

    public ItemViewModel(FolderViewViewModel viewModel, LayoutPreferencesManager folderSettingsViewModel)
    {
        folderSettings = folderSettingsViewModel;
        filesAndFolders = new ConcurrentCollection<ListedItem>();
        FilesAndFolders = new BulkConcurrentObservableCollection<ListedItem>();
        itemLoadQueue = new ConcurrentDictionary<string, bool>();
        addFilesCTS = new CancellationTokenSource();
        semaphoreCTS = new CancellationTokenSource();
        loadPropsCTS = new CancellationTokenSource();
        watcherCTS = new CancellationTokenSource();
        enumFolderSemaphore = new SemaphoreSlim(1, 1);
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        ViewModel = viewModel;
    }

    #region get base storage item

    public Task<FilesystemResult<BaseStorageFile>> GetFileFromPathAsync(string path)
        => FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path, workingRoot, currentStorageFolder!));

    public Task<FilesystemResult<BaseStorageFolder>> GetFolderFromPathAsync(string path)
        => FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, workingRoot, currentStorageFolder!));

    public Task<FilesystemResult<StorageFolderWithPath>> GetFolderWithPathFromPathAsync(string value)
            => FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(value, workingRoot, currentStorageFolder!));

    public Task<FilesystemResult<StorageFileWithPath>> GetFileWithPathFromPathAsync(string value)
        => FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(value, workingRoot, currentStorageFolder!));

    #endregion

    #region get default icons

    public Dictionary<string, BitmapImage> DefaultIcons = new();

    private uint currentDefaultIconSize = 0;

    private async Task GetDefaultItemIconsAsync(uint size)
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
    }

    #endregion

    #region update working directory

    public async Task SetWorkingDirectoryAsync(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var isLibrary = false;
        string? name = null;
        /*if (App.LibraryManager.TryGetLibrary(value, out LibraryLocationItem library))
        {
            isLibrary = true;
            name = library.Text;
        }*/

        WorkingDirectoryModified?.Invoke(this, new WorkingDirectoryModifiedEventArgs { Path = value, IsLibrary = isLibrary, Name = name });

        if (isLibrary || !Path.IsPathRooted(value))
        {
            workingRoot = currentStorageFolder = null!;
        }
        else if (!Path.IsPathRooted(WorkingDirectory) || Path.GetPathRoot(WorkingDirectory) != Path.GetPathRoot(value))
        {
            workingRoot = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(value));
        }

        /*if (value == "Home")
        {
            currentStorageFolder = null;
        }
        else
        {
            _ = Task.Run(() => jumpListService.AddFolderAsync(value));
        }*/

        WorkingDirectory = value;

        string? pathRoot = null;
        if (!FtpHelpers.IsFtpPath(WorkingDirectory))
        {
            pathRoot = Path.GetPathRoot(WorkingDirectory);
        }

        GitDirectory = GitHelpers.GetGitRepositoryPath(WorkingDirectory, pathRoot!);
        IsValidGitDirectory = !string.IsNullOrEmpty((await GitHelpers.GetRepositoryHead(GitDirectory))?.Name);
    }

    #endregion

    #region load and sort items

    private async Task RapidAddItemsToCollectionAsync(string? path, LibraryItem? library = null)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var isRecycleBin = path.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
        var enumerated = await EnumerateItemsFromStandardFolderAsync(path, null, library);

        // Hide progressbar after enumeration
        IsLoadingItems = false;

        switch (enumerated)
        {
            // Enumerated with FindFirstFileExFromApp
            // Is folder synced to cloud storage?
            case 0:
                currentStorageFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
                var syncStatus = await CheckCloudDriveSyncStatusAsync(currentStorageFolder?.Item!);

                PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs()
                {
                    IsTypeCloudDrive = syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown,
                    IsTypeGitRepository = IsValidGitDirectory
                });

                if (!HasNoWatcher)
                {
                    WatchForDirectoryChanges(path, syncStatus);
                }

                if (IsValidGitDirectory)
                {
                    WatchForGitChanges();
                }

                break;

            // Enumerated with StorageFolder
            case 1:
                PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = false, IsTypeRecycleBin = isRecycleBin });
                currentStorageFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
                if (!HasNoWatcher)
                {
                    await WatchForStorageFolderChangesAsync(currentStorageFolder?.Item);
                }

                break;

            // Watch for changes using Win32 in Box Drive folder (#7428) and network drives (#5869)
            case 2:
                PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = false });
                if (!HasNoWatcher)
                {
                    WatchForWin32FolderChanges(path);
                }

                break;

            // Enumeration failed
            case -1:
            default:
                break;
        }

        await GetDefaultItemIconsAsync(folderSettings.GetIconSize());

        if (IsLoadingCancelled)
        {
            IsLoadingCancelled = false;
            IsLoadingItems = false;
            return;
        }

        stopwatch.Stop();
    }

    private async Task<CloudDriveSyncStatus> CheckCloudDriveSyncStatusAsync(IStorageItem item)
    {
        int? syncStatus = null;
        if (item is BaseStorageFile file && file.Properties is not null)
        {
            var extraProperties = await FilesystemTasks.Wrap(() => file.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus" }).AsTask());
            if (extraProperties)
            {
                syncStatus = (int?)(uint?)extraProperties.Result["System.FilePlaceholderStatus"];
            }
        }
        else if (item is BaseStorageFolder folder && folder.Properties is not null)
        {
            var extraProperties = await FilesystemTasks.Wrap(() => folder.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus", "System.FileOfflineAvailabilityStatus" }).AsTask());
            if (extraProperties)
            {
                syncStatus = (int?)(uint?)extraProperties.Result["System.FileOfflineAvailabilityStatus"];

                // If no FileOfflineAvailabilityStatus, check FilePlaceholderStatus
                syncStatus ??= (int?)(uint?)extraProperties.Result["System.FilePlaceholderStatus"];
            }
        }

        return syncStatus is null || !Enum.IsDefined(typeof(CloudDriveSyncStatus), syncStatus)
            ? CloudDriveSyncStatus.Unknown
            : (CloudDriveSyncStatus)syncStatus;
    }

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

        HasNoWatcher = isFtp || isWslDistro || isMtp || currentStorageFolder?.Item is ZipStorageFolder;

        if (enumFromStorageFolder)
        {
            var basicProps = await rootFolder?.GetBasicPropertiesAsync();
            var currentFolder = library ?? new ListedItem(ViewModel)//rootFolder?.FolderRelativeId ?? string.Empty)
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
            await EnumFromStorageFolderAsync(path, rootFolder, currentStorageFolder!, cancellationToken);

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

            var currentFolder = library ?? new ListedItem(ViewModel)//null)
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
                await EnumFromStorageFolderAsync(path, rootFolder, currentStorageFolder!, cancellationToken);

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
                    var fileList = await Win32StorageEnumerator.ListEntries(ViewModel, path, hFile, findData, cancellationToken, -1, intermediateAction: async (intermediateList) =>
                    {
                        filesAndFolders.AddRange(intermediateList);
                        await OrderFilesAndFoldersAsync();
                        await ApplyFilesAndFoldersChangesAsync();
                    }, defaultIconPairs: DefaultIcons, showHiddenFile: ViewModel.GetSettings().ShowHiddenFile);

                    filesAndFolders.AddRange(fileList);

                    await OrderFilesAndFoldersAsync();
                    await ApplyFilesAndFoldersChangesAsync();

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
            var finalList = await UniversalStorageEnumerator.ListEntries(ViewModel, rootFolder, currentStorageFolder, cancellationToken, -1,
                async (intermediateList) =>
                {
                    filesAndFolders.AddRange(intermediateList);

                    await OrderFilesAndFoldersAsync();
                    await ApplyFilesAndFoldersChangesAsync();
                },
                defaultIconPairs: DefaultIcons);

            filesAndFolders.AddRange(finalList);

            await OrderFilesAndFoldersAsync();
            await ApplyFilesAndFoldersChangesAsync();
        }, cancellationToken == null ? CancellationToken.None : (CancellationToken)cancellationToken);

        if (rootFolder is IPasswordProtectedItem ppiu)
        {
            ppiu.PasswordRequestedCallback = null!;
        }
    }

    private Task OrderFilesAndFoldersAsync()
    {
        /*// Sorting group contents is handled elsewhere
        if (folderSettings.DirectoryGroupOption != GroupOption.None)
        {
            return Task.CompletedTask;
        }*/

        void OrderEntries()
        {
            if (filesAndFolders.Count == 0)
            {
                return;
            }

            // TODO: Add folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles
            var sortOption = SortOption.Name;
            var sortDirection = SortDirection.Ascending;
            var sortDirectoriesAlongsideFiles = false;
            filesAndFolders = new ConcurrentCollection<ListedItem>(SortingHelper.OrderFileList(filesAndFolders.ToList(), sortOption, sortDirection, sortDirectoriesAlongsideFiles));
        }
        if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent && dispatcherQueue.HasThreadAccess)
        {
            return Task.Run(OrderEntries);
        }

        OrderEntries();

        return Task.CompletedTask;
    }

    // Apply changes immediately after manipulating on filesAndFolders completed
    public async Task ApplyFilesAndFoldersChangesAsync()
    {
        try
        {
            if (filesAndFolders is null || filesAndFolders.Count == 0)
            {
                void ClearDisplay()
                {
                    FilesAndFolders.Clear();
                    //UpdateEmptyTextType();
                    DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
                }

                if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent && dispatcherQueue.HasThreadAccess)
                {
                    ClearDisplay();
                }
                else
                {
                    await dispatcherQueue.EnqueueOrInvokeAsync(ClearDisplay);
                }

                return;
            }
            var filesAndFoldersLocal = filesAndFolders.ToList();

            // CollectionChanged will cause UI update, which may cause significant performance degradation,
            // so suppress CollectionChanged event here while loading items heavily.

            // Note that both DataGrid and GridView don't support multi-items changes notification, so here
            // we have to call BeginBulkOperation to suppress CollectionChanged and call EndBulkOperation
            // in the end to fire a CollectionChanged event with NotifyCollectionChangedAction.Reset
            FilesAndFolders.BeginBulkOperation();

            // After calling BeginBulkOperation, ObservableCollection.CollectionChanged is suppressed
            // so modifies to FilesAndFolders won't trigger UI updates, hence below operations can be
            // run safely without needs of dispatching to UI thread
            void ApplyChanges()
            {
                var startIndex = -1;
                var tempList = new List<ListedItem>();

                void ApplyBulkInsertEntries()
                {
                    if (startIndex != -1)
                    {
                        FilesAndFolders.ReplaceRange(startIndex, tempList);
                        startIndex = -1;
                        tempList.Clear();
                    }
                }

                for (var i = 0; i < filesAndFoldersLocal.Count; i++)
                {
                    if (addFilesCTS.IsCancellationRequested)
                    {
                        return;
                    }

                    if (i < FilesAndFolders.Count)
                    {
                        if (FilesAndFolders[i] != filesAndFoldersLocal[i])
                        {
                            if (startIndex == -1)
                            {
                                startIndex = i;
                            }

                            tempList.Add(filesAndFoldersLocal[i]);
                        }
                        else
                        {
                            ApplyBulkInsertEntries();
                        }
                    }
                    else
                    {
                        ApplyBulkInsertEntries();
                        FilesAndFolders.InsertRange(i, filesAndFoldersLocal.Skip(i));

                        break;
                    }
                }

                ApplyBulkInsertEntries();

                if (FilesAndFolders.Count > filesAndFoldersLocal.Count)
                {
                    FilesAndFolders.RemoveRange(filesAndFoldersLocal.Count, FilesAndFolders.Count - filesAndFoldersLocal.Count);
                }

                /*if (folderSettings.DirectoryGroupOption != GroupOption.None)
                {
                    OrderGroups();
                }*/
            }

            void UpdateUI()
            {
                // Trigger CollectionChanged with NotifyCollectionChangedAction.Reset
                // once loading is completed so that UI can be updated
                FilesAndFolders.EndBulkOperation();
                //UpdateEmptyTextType();
                DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
            }

            if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent && dispatcherQueue.HasThreadAccess)
            {
                await Task.Run(ApplyChanges);
                UpdateUI();
            }
            else
            {
                ApplyChanges();
                await dispatcherQueue.EnqueueOrInvokeAsync(UpdateUI);
            }
        }
        catch (Exception)
        {
            
        }
    }

    #endregion

    #region watch for changes

    private void WatchForDirectoryChanges(string path, CloudDriveSyncStatus syncStatus)
    {
        /*var hWatchDir = NativeFileOperationsHelper.CreateFileFromApp(path, 1, 1 | 2 | 4,
            IntPtr.Zero, 3, (uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics | (uint)NativeFileOperationsHelper.File_Attributes.Overlapped, IntPtr.Zero);
        if (hWatchDir.ToInt64() == -1)
        {
            return;
        }

        var hasSyncStatus = syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown;

        aProcessQueueAction ??= Task.Factory.StartNew(() => ProcessOperationQueueAsync(watcherCTS.Token, hasSyncStatus), default,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);

        var aWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
        {
            var buff = new byte[4096];
            var rand = Guid.NewGuid();
            var notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_SIZE;

            if (hasSyncStatus)
            {
                notifyFilters |= FILE_NOTIFY_CHANGE_ATTRIBUTES;
            }

            var overlapped = new OVERLAPPED();
            overlapped.hEvent = CreateEvent(IntPtr.Zero, false, false, null);
            const uint INFINITE = 0xFFFFFFFF;

            while (x.Status != AsyncStatus.Canceled)
            {
                unsafe
                {
                    fixed (byte* pBuff = buff)
                    {
                        ref var notifyInformation = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[0]);
                        if (x.Status != AsyncStatus.Canceled)
                        {
                            ReadDirectoryChangesW(hWatchDir, pBuff,
                            4096, false,
                            notifyFilters, null,
                            ref overlapped, null);
                        }
                        else
                        {
                            break;
                        }

                        Debug.WriteLine("waiting: {0}", rand);
                        if (x.Status == AsyncStatus.Canceled)
                            break;

                        var rc = WaitForSingleObjectEx(overlapped.hEvent, INFINITE, true);
                        Debug.WriteLine("wait done: {0}", rand);

                        uint offset = 0;
                        ref var notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
                        if (x.Status == AsyncStatus.Canceled)
                            break;

                        do
                        {
                            notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
                            string? FileName = null;
                            unsafe
                            {
                                fixed (char* name = notifyInfo.FileName)
                                {
                                    FileName = Path.Combine(path, new string(name, 0, (int)notifyInfo.FileNameLength / 2));
                                }
                            }

                            uint action = notifyInfo.Action;

                            Debug.WriteLine("action: {0}", action);

                            operationQueue.Enqueue((action, FileName));

                            offset += notifyInfo.NextEntryOffset;
                        }
                        while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

                        operationEvent.Set();

                        //ResetEvent(overlapped.hEvent);
                    }
                }
            }

            CloseHandle(overlapped.hEvent);
            operationQueue.Clear();
        });

        watcherCTS.Token.Register(() =>
        {
            if (aWatcherAction is not null)
            {
                aWatcherAction?.Cancel();

                // Prevent duplicate execution of this block
                aWatcherAction = null;
            }

            CancelIoEx(hWatchDir, IntPtr.Zero);
            CloseHandle(hWatchDir);
        });*/
    }

    private void WatchForGitChanges()
    {
        /*var hWatchDir = NativeFileOperationsHelper.CreateFileFromApp(
            GitDirectory!,
            1,
            1 | 2 | 4,
            IntPtr.Zero,
            3,
            (uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics | (uint)NativeFileOperationsHelper.File_Attributes.Overlapped,
            IntPtr.Zero);

        if (hWatchDir.ToInt64() == -1)
        {
            return;
        }

        gitProcessQueueAction ??= Task.Factory.StartNew(() => ProcessGitChangesQueueAsync(watcherCTS.Token), default,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);

        var gitWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
        {
            var buff = new byte[4096];
            var rand = Guid.NewGuid();
            var notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_SIZE | FILE_NOTIFY_CHANGE_CREATION;

            var overlapped = new OVERLAPPED();
            overlapped.hEvent = CreateEvent(IntPtr.Zero, false, false, null);
            const uint INFINITE = 0xFFFFFFFF;

            while (x.Status != AsyncStatus.Canceled)
            {
                unsafe
                {
                    fixed (byte* pBuff = buff)
                    {
                        ref var notifyInformation = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[0]);
                        if (x.Status == AsyncStatus.Canceled)
                            break;

                        ReadDirectoryChangesW(hWatchDir, pBuff,
                            4096, true,
                            notifyFilters, null,
                            ref overlapped, null);

                        if (x.Status == AsyncStatus.Canceled)
                            break;

                        var rc = WaitForSingleObjectEx(overlapped.hEvent, INFINITE, true);

                        uint offset = 0;
                        ref var notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
                        if (x.Status == AsyncStatus.Canceled)
                            break;

                        do
                        {
                            notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);

                            uint action = notifyInfo.Action;

                            gitChangesQueue.Enqueue(action);

                            offset += notifyInfo.NextEntryOffset;
                        }
                        while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

                        gitChangedEvent.Set();
                    }
                }
            }

            CloseHandle(overlapped.hEvent);
            gitChangesQueue.Clear();
        });

        watcherCTS.Token.Register(() =>
        {
            if (gitWatcherAction is not null)
            {
                gitWatcherAction?.Cancel();

                // Prevent duplicate execution of this block
                gitWatcherAction = null;
            }

            CancelIoEx(hWatchDir, IntPtr.Zero);
            CloseHandle(hWatchDir);
        });*/
    }

    private async Task WatchForStorageFolderChangesAsync(BaseStorageFolder? rootFolder)
    {
        /*if (rootFolder is null)
        {
            return;
        }

        await Task.Factory.StartNew(() =>
        {
            var options = new QueryOptions()
            {
                FolderDepth = FolderDepth.Shallow,
                IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties
            };

            options.SetPropertyPrefetch(PropertyPrefetchOptions.None, null);
            options.SetThumbnailPrefetch(ThumbnailMode.ListView, 0, ThumbnailOptions.ReturnOnlyIfCached);

            if (rootFolder.AreQueryOptionsSupported(options))
            {
                var itemQueryResult = rootFolder.CreateItemQueryWithOptions(options).ToStorageItemQueryResult();
                itemQueryResult.ContentsChanged += ItemQueryResult_ContentsChanged;

                // Just get one item to start getting notifications
                var watchedItemsOperation = itemQueryResult.GetItemsAsync(0, 1);

                watcherCTS.Token.Register(() =>
                {
                    itemQueryResult.ContentsChanged -= ItemQueryResult_ContentsChanged;
                    watchedItemsOperation?.Cancel();
                });
            }
        },
        default,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default);*/
    }

    // Win32FolderChanges

    private void WatchForWin32FolderChanges(string? folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            if (watcher is null)
            {
                watcher = new FileSystemWatcher
                {
                    Path = folderPath,
                    Filter = "*.*",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
                };
            }
            else
            {
                watcher.EnableRaisingEvents = false;
                watcher.Path = folderPath;
            }

            watcher.Created += DirectoryWatcher_Changed;
            watcher.Deleted += DirectoryWatcher_Changed;
            watcher.Renamed += DirectoryWatcher_Changed;
            watcher.EnableRaisingEvents = true;
        }
    }

    private async void DirectoryWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
        {
            await RefreshItems(null);
        });
    }

    #endregion

    #region refresh items

    public async Task RefreshItems(string? previousDir, Action postLoadCallback = null!)
    {
        await RapidAddItemsToCollectionAsync(WorkingDirectory, previousDir, postLoadCallback);
    }

    private async Task RapidAddItemsToCollectionAsync(string path, string? previousDir, Action postLoadCallback)
    {
        IsSearchResults = false;
        ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting });

        CancelLoadAndClearFiles();

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            // Only one instance at a time should access this function
            // Wait here until the previous one has ended
            // If we're waiting and a new update request comes through simply drop this instance
            await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            // Drop all the other waiting instances
            semaphoreCTS.Cancel();
            semaphoreCTS = new CancellationTokenSource();

            IsLoadingItems = true;

            filesAndFolders.Clear();
            FilesAndFolders.Clear();

            ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress });

            if (path.ToLowerInvariant().EndsWith(ShellLibraryItem.EXTENSION, StringComparison.Ordinal))
            {
                /*if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library) && !library.IsEmpty)
                {
                    var libItem = new LibraryItem(library);
                    foreach (var folder in library.Folders)
                    {
                        await RapidAddItemsToCollectionAsync(folder, libItem);
                    }
                }*/
            }
            else
            {
                await RapidAddItemsToCollectionAsync(path);
            }

            ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete, PreviousDirectory = previousDir, Path = path });
            IsLoadingItems = false;

            //AdaptiveLayoutHelpers.ApplyAdaptativeLayout(folderSettings, WorkingDirectory, filesAndFolders.ToList());
        }
        finally
        {
            // Make sure item count is updated
            DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
            enumFolderSemaphore.Release();
        }

        postLoadCallback?.Invoke();
    }

    public void CancelExtendedPropertiesLoadingForItem(ListedItem item)
    {
        itemLoadQueue.TryUpdate(item.ItemPath, true, false);
    }

    public async Task LoadExtendedItemPropertiesAsync(ListedItem item, uint thumbnailSize = 20)
    {
        if (item is null)
        {
            return;
        }

        itemLoadQueue[item.ItemPath] = false;

        var cts = loadPropsCTS;

        try
        {
            await Task.Run(async () =>
            {
                if (itemLoadQueue.TryGetValue(item.ItemPath, out var canceled) && canceled)
                {
                    return;
                }

                item.ItemPropertiesInitialized = true;
                var wasSyncStatusLoaded = false;
                var loadGroupHeaderInfo = false;
                ImageSource? groupImage = null;
                GroupedCollection<ListedItem>? gp = null;
                try
                {
                    BaseStorageFile? matchingStorageFile = null;
                    if (item.Key is not null && FilesAndFolders.IsGrouped && FilesAndFolders.GetExtendedGroupHeaderInfo is not null)
                    {
                        gp = FilesAndFolders.GroupedCollection?.Where(x => x.Model.Key == item.Key).FirstOrDefault();
                        loadGroupHeaderInfo = gp is not null && !gp.Model.Initialized && gp.GetExtendedGroupHeaderInfo is not null;
                    }

                    if (item.IsLibrary || item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
                    {
                        if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            matchingStorageFile = await GetFileFromPathAsync(item.ItemPath);
                            if (matchingStorageFile is not null)
                            {
                                cts.Token.ThrowIfCancellationRequested();
                                await LoadItemThumbnailAsync(item, thumbnailSize, matchingStorageFile);

                                var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageFile);
                                /*var fileFRN = await FileTagsHelper.GetFileFRN(matchingStorageFile);
                                var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);*/
                                var itemType = (item.ItemType == "Folder".GetLocalized()) ? item.ItemType : matchingStorageFile.DisplayType;
                                cts.Token.ThrowIfCancellationRequested();

                                await dispatcherQueue.EnqueueOrInvokeAsync(() =>
                                {
                                    /*item.FolderRelativeId = matchingStorageFile.FolderRelativeId;*/
                                    item.ItemType = itemType;
                                    item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                    /*item.FileFRN = fileFRN;
                                    item.FileTags = fileTag;*/
                                }, DispatcherQueuePriority.Low);

                                /*SetFileTag(item);*/
                                wasSyncStatusLoaded = true;
                            }
                        }

                        if (!wasSyncStatusLoaded)
                        {
                            await LoadItemThumbnailAsync(item, thumbnailSize, null);
                        }
                    }
                    else
                    {
                        if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            BaseStorageFolder matchingStorageFolder = await GetFolderFromPathAsync(item.ItemPath);
                            if (matchingStorageFolder is not null)
                            {
                                cts.Token.ThrowIfCancellationRequested();
                                await LoadItemThumbnailAsync(item, thumbnailSize, matchingStorageFolder);
                                if (matchingStorageFolder.DisplayName != item.Name && !matchingStorageFolder.DisplayName.StartsWith("$R", StringComparison.Ordinal))
                                {
                                    cts.Token.ThrowIfCancellationRequested();
                                    await dispatcherQueue.EnqueueOrInvokeAsync(() =>
                                    {
                                        item.ItemNameRaw = matchingStorageFolder.DisplayName;
                                    });
                                    await fileListCache.SaveFileDisplayNameToCache(item.ItemPath, matchingStorageFolder.DisplayName);
                                    if (/*folderSettings.DirectorySortOption == SortOption.Name && */!isLoadingItems)
                                    {
                                        await OrderFilesAndFoldersAsync();
                                        await ApplySingleFileChangeAsync(item);
                                    }
                                }

                                cts.Token.ThrowIfCancellationRequested();
                                var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageFolder);
                                /*var fileFRN = await FileTagsHelper.GetFileFRN(matchingStorageFolder);
                                var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);*/
                                var itemType = (item.ItemType == "Folder".GetLocalized()) ? item.ItemType : matchingStorageFolder.DisplayType;
                                cts.Token.ThrowIfCancellationRequested();

                                await dispatcherQueue.EnqueueOrInvokeAsync(() =>
                                {
                                    //item.FolderRelativeId = matchingStorageFolder.FolderRelativeId;
                                    item.ItemType = itemType;
                                    item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                    /*item.FileFRN = fileFRN;
                                    item.FileTags = fileTag;*/
                                }, DispatcherQueuePriority.Low);

                                /*SetFileTag(item);*/
                                wasSyncStatusLoaded = true;
                            }
                        }
                        if (!wasSyncStatusLoaded)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            await LoadItemThumbnailAsync(item, thumbnailSize, null);
                        }
                    }

                    /*var isFileTypeGroupMode = folderSettings.DirectoryGroupOption == GroupOption.FileType;
                    if (loadGroupHeaderInfo && isFileTypeGroupMode)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        groupImage = await GetItemTypeGroupIcon(item, matchingStorageFile);
                    }*/
                }
                catch (Exception)
                {
                }
                finally
                {
                    if (!wasSyncStatusLoaded)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        await FilesystemTasks.Wrap(async () =>
                        {
                            /*var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);*/

                            await dispatcherQueue.EnqueueOrInvokeAsync(() =>
                            {
                                // Reset cloud sync status icon
                                item.SyncStatusUI = new CloudDriveSyncStatusUI();

                                /*item.FileTags = fileTag;*/
                            }, DispatcherQueuePriority.Low);

                            /*SetFileTag(item);*/
                        });
                    }

                    if (loadGroupHeaderInfo)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        await SafetyExtensions.IgnoreExceptions(() =>
                            dispatcherQueue.EnqueueOrInvokeAsync(() =>
                            {
                                gp!.Model.ImageSource = groupImage!;
                                gp.InitializeExtendedGroupHeaderInfoAsync();
                            }));
                    }
                }
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Ignored
        }
        finally
        {
            itemLoadQueue.TryRemove(item.ItemPath, out _);
        }
    }

    // ThumbnailSize is set to 96 so that unless we override it, mode is in turn set to SingleItem
    private async Task LoadItemThumbnailAsync(ListedItem item, uint thumbnailSize = 96, IStorageItem? matchingStorageItem = null)
    {
        var wasIconLoaded = false;
        if (item.IsLibrary || item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
        {
            if (ViewModel.GetSettings().ShowThumbnail && !item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
            {
                var matchingStorageFile = matchingStorageItem?.AsBaseStorageFile() ?? await GetFileFromPathAsync(item.ItemPath);

                if (matchingStorageFile is not null)
                {
                    // SingleItem returns image thumbnails in the correct aspect ratio for the grid layouts
                    // ListView is used for the details and columns layout
                    var thumbnailMode = thumbnailSize < 96 ? ThumbnailMode.ListView : ThumbnailMode.SingleItem;

                    using StorageItemThumbnail Thumbnail = await FilesystemTasks.Wrap(() => matchingStorageFile.GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ResizeThumbnail).AsTask());

                    if (!(Thumbnail is null || Thumbnail.Size == 0 || Thumbnail.OriginalHeight == 0 || Thumbnail.OriginalWidth == 0))
                    {
                        await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
                        {
                            var img = new BitmapImage
                            {
                                DecodePixelType = DecodePixelType.Logical,
                                DecodePixelWidth = (int)thumbnailSize
                            };
                            await img.SetSourceAsync(Thumbnail);
                            item.FileImage = img;
                            if (!string.IsNullOrEmpty(item.FileExtension) &&
                                !item.IsShortcut && !item.IsExecutable &&
                                !ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant()))
                            {
                                DefaultIcons.AddIfNotPresent(item.FileExtension.ToLowerInvariant(), item.FileImage);
                            }
                        }, DispatcherQueuePriority.Low);
                        wasIconLoaded = true;
                    }

                    var overlayInfo = await FileThumbnailHelper.LoadOverlayAsync(item.ItemPath, thumbnailSize);
                    if (overlayInfo is not null)
                    {
                        await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
                        {
                            item.IconOverlay = await overlayInfo.ToBitmapAsync();
                            item.ShieldIcon = await GetShieldIcon();
                        }, DispatcherQueuePriority.Low);
                    }
                }
            }

            if (!wasIconLoaded)
            {
                byte[] IconData = null!;
                byte[] OverlayData = null!;
                if (ViewModel.GetSettings().ShowIconOverlay)
                {
                    (IconData, OverlayData) = await FileThumbnailHelper.LoadIconAndOverlayAsync(item.ItemPath, thumbnailSize, false);
                }
                else
                {
                    IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(item.ItemPath, thumbnailSize, false);
                }
                
                if (IconData is not null)
                {
                    await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
                    {
                        item.FileImage = await IconData.ToBitmapAsync();
                        if (!string.IsNullOrEmpty(item.FileExtension) &&
                            !item.IsShortcut && !item.IsExecutable &&
                            !ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant()))
                        {
                            DefaultIcons!.AddIfNotPresent(item.FileExtension.ToLowerInvariant(), item.FileImage);
                        }
                    }, DispatcherQueuePriority.Low);
                }

                if (OverlayData is not null)
                {
                    await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
                    {
                        item.IconOverlay = await OverlayData.ToBitmapAsync();
                        item.ShieldIcon = await GetShieldIcon();
                    }, DispatcherQueuePriority.Low);
                }
            }
        }
        else
        {
            if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
            {
                var matchingStorageFolder = matchingStorageItem?.AsBaseStorageFolder() ?? await GetFolderFromPathAsync(item.ItemPath);
                if (matchingStorageFolder is not null)
                {
                    // SingleItem returns image thumbnails in the correct aspect ratio for the grid layouts
                    // ListView is used for the details and columns layout
                    var thumbnailMode = thumbnailSize < 96 ? ThumbnailMode.ListView : ThumbnailMode.SingleItem;

                    // We use ReturnOnlyIfCached because otherwise folders thumbnails have a black background, this has the downside the folder previews don't work
                    using StorageItemThumbnail Thumbnail = await FilesystemTasks.Wrap(() => matchingStorageFolder.GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ReturnOnlyIfCached).AsTask());
                    if (!(Thumbnail is null || Thumbnail.Size == 0 || Thumbnail.OriginalHeight == 0 || Thumbnail.OriginalWidth == 0))
                    {
                        await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
                        {
                            var img = new BitmapImage
                            {
                                DecodePixelType = DecodePixelType.Logical,
                                DecodePixelWidth = (int)thumbnailSize
                            };
                            await img.SetSourceAsync(Thumbnail);
                            item.FileImage = img;
                        }, DispatcherQueuePriority.Normal);
                        wasIconLoaded = true;
                    }

                    var overlayInfo = await FileThumbnailHelper.LoadOverlayAsync(item.ItemPath, thumbnailSize);
                    if (overlayInfo is not null)
                    {
                        await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
                        {
                            item.IconOverlay = await overlayInfo.ToBitmapAsync();
                            item.ShieldIcon = await GetShieldIcon();
                        }, DispatcherQueuePriority.Low);
                    }
                }
            }

            if (!wasIconLoaded)
            {
                byte[] IconData = null!;
                byte[] OverlayData = null!;
                if (ViewModel.GetSettings().ShowIconOverlay)
                {
                    (IconData, OverlayData) = await FileThumbnailHelper.LoadIconAndOverlayAsync(item.ItemPath, thumbnailSize, false);
                }
                else
                {
                    IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(item.ItemPath, thumbnailSize, false);
                }

                if (IconData is not null)
                {
                    await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
                    {
                        item.FileImage = await IconData.ToBitmapAsync();
                    }, DispatcherQueuePriority.Low);
                }

                if (OverlayData is not null)
                {
                    await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
                    {
                        item.IconOverlay = await OverlayData.ToBitmapAsync();
                        item.ShieldIcon = await GetShieldIcon();
                    }, DispatcherQueuePriority.Low);
                }
            }
        }
    }

    private async Task<BitmapImage> GetShieldIcon()
    {
        shieldIcon ??= (await UIHelpers.GetShieldIconResource())!;

        return shieldIcon!;
    }

    public async Task ApplySingleFileChangeAsync(ListedItem item)
    {
        var newIndex = filesAndFolders.IndexOf(item);
        await dispatcherQueue.EnqueueOrInvokeAsync(() =>
        {
            FilesAndFolders.Remove(item);
            if (newIndex != -1)
            {
                FilesAndFolders.Insert(Math.Min(newIndex, FilesAndFolders.Count), item);
            }

            /*if (folderSettings.DirectoryGroupOption != GroupOption.None)
            {
                var key = FilesAndFolders.ItemGroupKeySelector?.Invoke(item);
                var group = FilesAndFolders.GroupedCollection?.FirstOrDefault(x => x.Model.Key == key);
                group?.OrderOne(list => SortingHelper.OrderFileList(list, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles), item);
            }*/

            //UpdateEmptyTextType();
            DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
        });
    }

    #endregion

    #region dispose methods

    public void Dispose()
    {
        CancelLoadAndClearFiles();
        DefaultIcons.Clear();
    }

    public void CancelLoadAndClearFiles()
    {
        CloseWatcher();
        if (IsLoadingItems)
        {
            IsLoadingCancelled = true;
            addFilesCTS.Cancel();
            addFilesCTS = new CancellationTokenSource();
        }
        //CancelExtendedPropertiesLoading();
        filesAndFolders.Clear();
        FilesAndFolders.Clear();
        //CancelSearch();
    }

    public void CloseWatcher()
    {
        watcher?.Dispose();
        watcher = null!;

        aProcessQueueAction = null;
        gitProcessQueueAction = null;
        watcherCTS?.Cancel();
        watcherCTS = new CancellationTokenSource();
    }

    #endregion

    #region event arguments

    public class PageTypeUpdatedEventArgs
    {
        public bool IsTypeCloudDrive
        {
            get; set;
        }

        public bool IsTypeRecycleBin
        {
            get; set;
        }

        public bool IsTypeGitRepository
        {
            get; set;
        }

        public bool IsTypeSearchResults
        {
            get; set;
        }
    }

    public class WorkingDirectoryModifiedEventArgs : EventArgs
    {
        public string? Path
        {
            get; set;
        }

        public string? Name
        {
            get; set;
        }

        public bool IsLibrary
        {
            get; set;
        }
    }

    public class ItemLoadStatusChangedEventArgs : EventArgs
    {
        public enum ItemLoadStatus
        {
            Starting,
            InProgress,
            Complete
        }

        public ItemLoadStatus Status
        {
            get; set;
        }

        /// <summary>
        /// This property may not be provided consistently if Status is not Complete
        /// </summary>
        public string? PreviousDirectory
        {
            get; set;
        }

        /// <summary>
        /// This property may not be provided consistently if Status is not Complete
        /// </summary>
        public string? Path
        {
            get; set;
        }
    }

    #endregion
}