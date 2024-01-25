using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;
using Files.App.Utils;
using Files.App;
using Files.App.Data.Commands;
using Files.App.Data.Models;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.App.ViewModels.Layouts;
using Files.App.Data.EventArguments;
using Microsoft.UI.Xaml.Data;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Views.Windows;
using Files.Core.Data.Enums;
using Files.Core.Services;
using Files.App.Data.Items;
using Files.App.Utils.Cloud;
using Microsoft.UI.Xaml.Controls;
using DesktopWidgets3.Helpers;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.FileProperties;
using Files.App.Data.Contexts;
using System.ComponentModel;
using static Files.App.Data.Models.ItemViewModel;
using static Files.App.Data.EventArguments.NavigationArguments;
using Files.App.Utils.Shell;
using Files.Core.ViewModels.FolderView;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.ViewModels.Pages.Widget;

public partial class FolderViewViewModel : BaseWidgetViewModel<FolderViewWidgetSettings>, IWidgetUpdate, IWidgetClose, IFolderViewViewModel
{
    #region view properties

    [ObservableProperty]
    public CollectionViewSource _collectionViewSource = new()
    {
        IsSourceGrouped = false,
    };

    [ObservableProperty]
    private string _header = string.Empty;

    [ObservableProperty]
    private string _toolTipText = string.Empty;

    [ObservableProperty]
    private bool _loadingIndicatorStatus = false;

    [ObservableProperty]
    private bool _canGoBack = false;

    [ObservableProperty]
    private bool _canNavigateToParent = false;

    [ObservableProperty]
    private bool _canRefresh = true;

    [ObservableProperty]
    private ImageSource? _imageSource = null;

    [ObservableProperty]
    private bool _allowNavigation = true;

    #endregion

    #region settings

    private RefreshBehaviours refreshBehaviour;

    private string FolderPath = string.Empty;

    private bool ShowIconOverlay = true;

    private bool ShowHiddenFile = false;

    private bool ShowExtension = false;

    private DeleteConfirmationPolicies DeleteConfirmationPolicy = DeleteConfirmationPolicies.Always;

    private bool ShowThumbnail = true;

    private FileNameConflictResolveOptionType ConflictsResolveOption = FileNameConflictResolveOptionType.GenerateNewName;

    #endregion

    #region current path

    public string CurFolderPath => FileSystemViewModel.WorkingDirectory;

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

                OnPropertyChanged(nameof(IsItemSelected));
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
                    SelectedItemsPropertiesViewModel.IsItemSelected = false;
                }
                else if (selectedItems is not null)
                {
                    IsItemSelected = true;
                    SelectedItem = selectedItems.First();
                    SelectedItemsPropertiesViewModel.IsItemSelected = true;
                }

                HasSelection = SelectedItems.Count != 0;

                OnPropertyChanged(nameof(SelectedItems));
            }
        }
    }

    #endregion

    #region rename items

    public bool IsRenamingItem { get; set; }

    public ListedItem? RenamingItem { get; set; }

    public string? OldItemName { get; set; }

    #endregion

    #region dialog

    [ObservableProperty]
    public static bool _canShowDialog = true;

    #endregion

    #region page type

    private ContentPageTypes pageType = ContentPageTypes.None;
    public ContentPageTypes PageType => pageType;

    #endregion

    #region create items

    public bool CanCreateItem => GetCanCreateItem();

    #endregion

    #region models from Files

    public ItemViewModel FileSystemViewModel;

    public CurrentInstanceViewModel InstanceViewModel;

    public BaseLayoutViewModel CommandsViewModel;

    public ItemManipulationModel ItemManipulationModel;

    public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel;

    public ICommandManager CommandManager => _commandManager;

    public IFileSystemHelpers FileSystemHelpers => _fileSystemHelpers;

    public LayoutPreferencesManager FolderSettings => InstanceViewModel.FolderSettings;

    public FolderViewViewModel ToolbarViewModel => this;

    public IDialogService DialogService => _dialogService;

    public DrivesViewModel DrivesViewModel => _drivesViewModel;

    public NetworkDrivesViewModel NetworkDrivesViewModel => _networkDrivesViewModel;

    #endregion

    private readonly ICommandManager _commandManager;
    private readonly IDialogService _dialogService;
    private readonly DrivesViewModel _drivesViewModel;
    private readonly IFileSystemHelpers _fileSystemHelpers;
    private readonly NetworkDrivesViewModel _networkDrivesViewModel;
    private readonly IWidgetManagerService _widgetManagerService;

    FileNameConflictResolveOptionType IFolderViewViewModel.ConflictsResolveOption => ConflictsResolveOption;

    bool IFolderViewViewModel.ShowFileExtensions => ShowExtension;

    bool IFolderViewViewModel.ShowDotFiles => true;

    public string WorkingDirectory => FileSystemViewModel.WorkingDirectory;

    public Window MainWindow => WidgetWindow;

    public IntPtr WindowHandle => WidgetWindow.WindowHandle;

    public FolderViewViewModel(ICommandManager commandManager, IDialogService dialogService, DrivesViewModel drivesViewModel, IFileSystemHelpers fileSystemHelpers, NetworkDrivesViewModel networkDrivesViewModel, IWidgetManagerService widgetManagerService)
    {
        _commandManager = commandManager;
        _commandManager.Initialize(this);
        _dialogService = dialogService;
        _dialogService.Initialize(this);
        _drivesViewModel = drivesViewModel;
        _fileSystemHelpers = fileSystemHelpers;
        _networkDrivesViewModel = networkDrivesViewModel;
        _widgetManagerService = widgetManagerService;

        NavigatedTo += FolderViewViewModel_NavigatedTo;

        InstanceViewModel = new();
        CommandsViewModel = new(this);
        ItemManipulationModel = new();
        SelectedItemsPropertiesViewModel = new();
        FileSystemViewModel = new(this, FolderSettings);

        FileSystemViewModel.ItemLoadStatusChanged += ItemViewModel_ItemLoadStatusChanged;
        InstanceViewModel.PropertyChanged += InstanceViewModel_PropertyChanged;
    }

    #region refresh items

    private async void FolderViewViewModel_NavigatedTo(object? parameter, bool isInitialized)
    {
        if (parameter is FolderViewWidgetSettings settings)
        {
            // Prepare navigation arguments
            parameter = new NavigationArguments()
            {
                NavPathParam = settings.FolderPath,
                PushFolderPath = false,
                RefreshBehaviour = refreshBehaviour
            };
        }

        if (parameter is NavigationArguments navigationArguments)
        {
            if (navigationArguments.RefreshBehaviour == RefreshBehaviours.NavigateToPath)
            {
                // Git properties are not loaded by default
                //ItemViewModel.EnabledGitProperties = GitProperties.None;

                //InitializeCommandsViewModel();

                IsItemSelected = false;

                /*FolderSettings!.LayoutModeChangeRequested += BaseFolderSettings_LayoutModeChangeRequested;
                FolderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
                FolderSettings.GroupDirectionPreferenceUpdated += FolderSettings_GroupDirectionPreferenceUpdated;
                FolderSettings.GroupByDateUnitPreferenceUpdated += FolderSettings_GroupByDateUnitPreferenceUpdated;*/

                /*ItemViewModel.EmptyTextType = EmptyTextType.None;*/
                ToolbarViewModel.CanRefresh = true;

                if (!navigationArguments.IsSearchResultPage)
                {
                    var previousDir = FileSystemViewModel.WorkingDirectory;
                    await FileSystemViewModel.SetWorkingDirectoryAsync(navigationArguments.NavPathParam);

                    // pathRoot will be empty on recycle bin path
                    var workingDir = FileSystemViewModel.WorkingDirectory ?? string.Empty;
                    var pathRoot = PathNormalization.GetPathRoot(workingDir);
                    var isRoot = workingDir == pathRoot;

                    var isRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
                    InstanceViewModel.IsPageTypeRecycleBin = isRecycleBin;

                    // Can't go up from recycle bin
                    ToolbarViewModel.CanNavigateToParent = (!(string.IsNullOrEmpty(pathRoot) || isRoot || isRecycleBin)) && AllowNavigation;

                    InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
                    InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
                    InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
                    //InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
                    InstanceViewModel.IsPageTypeSearchResults = false;
                    //ToolbarViewModel.PathControlDisplayText = navigationArguments.NavPathParam;

                    /*if (InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.Path)
                    {
                        InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.Name;
                    }

                    if (InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FolderPath &&
                        !InstanceViewModel.IsPageTypeLibrary)
                    {
                        InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.None;
                    }*/

                    if (!navigationArguments.IsLayoutSwitch || previousDir != workingDir)
                    {
                        await FileSystemViewModel.RefreshItems(previousDir);//, SetSelectedItemsOnNavigation);
                    }
                    /*else
                    {
                        ToolbarViewModel.CanGoForward = false;
                    }*/
                }
                else
                {
                    await FileSystemViewModel.SetWorkingDirectoryAsync(navigationArguments.SearchPathParam);

                    //ToolbarViewModel.CanGoForward = false;
                    ToolbarViewModel.CanNavigateToParent = false;

                    var workingDir = FileSystemViewModel.WorkingDirectory ?? string.Empty;

                    InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
                    InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
                    InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
                    InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
                    //InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
                    InstanceViewModel.IsPageTypeSearchResults = true;

                    /*if (!navigationArguments.IsLayoutSwitch)
                    {
                        var displayName = App.LibraryManager.TryGetLibrary(navigationArguments.SearchPathParam, out var lib) ? lib.Text : navigationArguments.SearchPathParam;
                        ParentShellPageInstance.UpdatePathUIToWorkingDirectory(null, string.Format("SearchPagePathBoxOverrideText".GetLocalizedResource(), navigationArguments.SearchQuery, displayName));
                        var searchInstance = new Utils.Storage.FolderSearch
                        {
                            Query = navigationArguments.SearchQuery,
                            Folder = navigationArguments.SearchPathParam,
                            ThumbnailSize = InstanceViewModel!.FolderSettings.GetIconSize(),
                            SearchUnindexedItems = navigationArguments.SearchUnindexedItems
                        };

                        _ = ItemViewModel.SearchAsync(searchInstance);
                    }*/
                }

                // Show controls that were hidden on the home page
                InstanceViewModel.IsPageTypeNotHome = true;
                //ItemViewModel.UpdateGroupOptions();

                UpdateCollectionViewSource();
                //InstanceViewModel.FolderSettings.IsLayoutModeChanging = false;

                //SetSelectedItemsOnNavigation();

                if (navigationArguments.PushFolderPath)
                {
                    navigationFolderPaths.Push(CurFolderPath);
                }
                ToolbarViewModel.CanGoBack = navigationFolderPaths.Count > 0 && AllowNavigation;

                await UpdateToolbarTextInfoAsync(navigationArguments);
            }
            else if (navigationArguments.RefreshBehaviour == RefreshBehaviours.RefreshItems)
            {
                await FileSystemViewModel.RefreshItems(null);
            }
        }

        if (!isInitialized)
        {
            var addItemService = App.GetService<IAddItemService>();
            await Task.WhenAll(
                addItemService.InitializeAsync(),
                ContextMenu.WarmUpQueryContextMenuAsync()
            );
        }
    }

    private void UpdateCollectionViewSource()
    {
        var items = FileSystemViewModel.FilesAndFolders;
        if (items.IsGrouped)
        {
            CollectionViewSource = new()
            {
                IsSourceGrouped = true,
                Source = items.GroupedCollection
            };
        }
        else
        {
            CollectionViewSource = new()
            {
                IsSourceGrouped = false,
                Source = items
            };
        }
    }

    public async Task RefreshIfNoWatcherExistsAsync()
    {
        if (FileSystemViewModel.HasNoWatcher)
        {
            await Refresh_Click();
        }
    }

    #endregion

    #region item view model events

    private void ItemViewModel_ItemLoadStatusChanged(object sender, ItemLoadStatusChangedEventArgs e)
    {
        switch (e.Status)
        {
            case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting:
                ToolbarViewModel.CanRefresh = false;
                SetLoadingIndicatorForTabs(true);
                break;
            case ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress:
                /*var columnCanNavigateBackward = false;
                var columnCanNavigateForward = false;
                if (SlimContentPage is ColumnsLayoutPage browser)
                {
                    columnCanNavigateBackward = browser.ParentShellPageInstance?.CanNavigateBackward ?? false;
                    columnCanNavigateForward = browser.ParentShellPageInstance?.CanNavigateForward ?? false;
                }
                ToolbarViewModel.CanGoBack = ItemDisplay.CanGoBack || columnCanNavigateBackward;
                ToolbarViewModel.CanGoForward = ItemDisplay.CanGoForward || columnCanNavigateForward;*/
                SetLoadingIndicatorForTabs(true);
                break;
            case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete:
                SetLoadingIndicatorForTabs(false);
                ToolbarViewModel.CanRefresh = true;
                // Select previous directory
                if (!string.IsNullOrWhiteSpace(e.PreviousDirectory) &&
                    e.PreviousDirectory.Contains(e.Path!, StringComparison.Ordinal) &&
                    !e.PreviousDirectory.Contains(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
                {
                    // Remove the WorkingDir from previous dir
                    e.PreviousDirectory = e.PreviousDirectory.Replace(e.Path!, string.Empty, StringComparison.Ordinal);

                    // Get previous dir name
                    if (e.PreviousDirectory.StartsWith('\\'))
                    {
                        e.PreviousDirectory = e.PreviousDirectory.Remove(0, 1);
                    }

                    if (e.PreviousDirectory.Contains('\\'))
                    {
                        e.PreviousDirectory = e.PreviousDirectory.Split('\\')[0];
                    }

                    // Get the first folder and combine it with WorkingDir
                    var folderToSelect = string.Format("{0}\\{1}", e.Path, e.PreviousDirectory);

                    // Make sure we don't get double \\ in the e.Path
                    folderToSelect = folderToSelect.Replace("\\\\", "\\", StringComparison.Ordinal);

                    if (folderToSelect.EndsWith('\\'))
                    {
                        folderToSelect = folderToSelect.Remove(folderToSelect.Length - 1, 1);
                    }

                    var itemToSelect = FileSystemViewModel.FilesAndFolders.Where((item) => item.ItemPath == folderToSelect).FirstOrDefault();

                    if (itemToSelect is not null)
                    {
                        ItemManipulationModel.SetSelectedItem(itemToSelect);
                        ItemManipulationModel.ScrollIntoView(itemToSelect);
                    }
                }
                break;
        }
    }

    #endregion

    #region instance view model events

    private void InstanceViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CurrentInstanceViewModel.IsPageTypeNotHome):
            case nameof(CurrentInstanceViewModel.IsPageTypeRecycleBin):
            case nameof(CurrentInstanceViewModel.IsPageTypeZipFolder):
            case nameof(CurrentInstanceViewModel.IsPageTypeFtp):
            case nameof(CurrentInstanceViewModel.IsPageTypeLibrary):
            case nameof(CurrentInstanceViewModel.IsPageTypeCloudDrive):
            case nameof(CurrentInstanceViewModel.IsPageTypeMtpDevice):
            case nameof(CurrentInstanceViewModel.IsPageTypeSearchResults):
                UpdatePageType();
                break;
            case nameof(CurrentInstanceViewModel.ShowSearchUnindexedItemsMessage):
                //OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
                break;
            case nameof(CurrentInstanceViewModel.IsGitRepository):
                //OnPropertyChanged(nameof(IsGitRepository));
                //OnPropertyChanged(nameof(CanExecuteGitAction));
                break;
        }
    }

    private void UpdatePageType()
    {
        var type = InstanceViewModel switch
        {
            null => ContentPageTypes.None,
            { IsPageTypeNotHome: false } => ContentPageTypes.Home,
            { IsPageTypeRecycleBin: true } => ContentPageTypes.RecycleBin,
            { IsPageTypeZipFolder: true } => ContentPageTypes.ZipFolder,
            { IsPageTypeFtp: true } => ContentPageTypes.Ftp,
            { IsPageTypeLibrary: true } => ContentPageTypes.Library,
            { IsPageTypeCloudDrive: true } => ContentPageTypes.CloudDrive,
            { IsPageTypeMtpDevice: true } => ContentPageTypes.MtpDevice,
            { IsPageTypeSearchResults: true } => ContentPageTypes.SearchResults,
            _ => ContentPageTypes.Folder,
        };
        SetProperty(ref pageType, type, nameof(PageType));
        OnPropertyChanged(nameof(CanCreateItem));
    }

    private bool GetCanCreateItem()
    {
        return /*ShellPage is not null &&*/
            pageType is not ContentPageTypes.None
            and not ContentPageTypes.Home
            and not ContentPageTypes.RecycleBin
            and not ContentPageTypes.ZipFolder
            and not ContentPageTypes.SearchResults
            and not ContentPageTypes.MtpDevice;
    }

    #endregion

    #region update toolbar

    private async Task UpdateToolbarTextInfoAsync(object parameter)
    {
        string header = null!;
        string toolTipText = null!;

        if (parameter is NavigationArguments navigationArguments)
        {
            (header, toolTipText) = await GetToolbarTextInfoAsync(navigationArguments.NavPathParam!);
        }

        // Don't update tabItem if the contents of the tab have already changed
        if (header is not null)
        {
            Header = header;
            (Header, ToolTipText) = (header, toolTipText);
        }
    }

    private async Task<(string tabLocationHeader, string toolTipText)> GetToolbarTextInfoAsync(string currentPath)
    {
        string? tabLocationHeader;
        var toolTipText = currentPath;

        if (currentPath.Equals(Constants.UserEnvironmentPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
        {
            tabLocationHeader = "Desktop".GetLocalized();
        }
        else if (currentPath.Equals(Constants.UserEnvironmentPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
        {
            tabLocationHeader = "Downloads".GetLocalized();
        }
        else if (currentPath.Equals(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
        {
            tabLocationHeader = "RecycleBin".GetLocalized();
        }
        else if (currentPath.Equals(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
        {
            tabLocationHeader = "ThisPC".GetLocalized();
        }
        else if (currentPath.Equals(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            tabLocationHeader = "SidebarNetworkDrives".GetLocalized();
        }
        /*else if (App.LibraryManager.TryGetLibrary(currentPath, out LibraryLocationItem library))
        {
            var libName = System.IO.Path.GetFileNameWithoutExtension(library.Path).GetLocalizedResource();
            // If localized string is empty use the library name.
            tabLocationHeader = string.IsNullOrEmpty(libName) ? library.Text : libName;
        }*/
        /*else if (WSLDistroManager.TryGetDistro(currentPath, out WslDistroItem? wslDistro) && currentPath.Equals(wslDistro.Path))
        {
            tabLocationHeader = wslDistro.Text;
        }*/
        else
        {
            var normalizedCurrentPath = PathNormalization.NormalizePath(currentPath);
            var matchingCloudDrive = CloudDrivesManager.Drives.FirstOrDefault(x => normalizedCurrentPath.Equals(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
            if (matchingCloudDrive is not null)
            {
                tabLocationHeader = matchingCloudDrive.Text;
            }
            else if (PathNormalization.NormalizePath(PathNormalization.GetPathRoot(currentPath)) == normalizedCurrentPath) // If path is a drive's root
            {
                var matchingDrive = NetworkDrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(netDrive => normalizedCurrentPath.Contains(PathNormalization.NormalizePath(netDrive.Path), StringComparison.OrdinalIgnoreCase));
                matchingDrive ??= DrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(drive => normalizedCurrentPath.Contains(PathNormalization.NormalizePath(drive.Path), StringComparison.OrdinalIgnoreCase));
                tabLocationHeader = matchingDrive is not null ? matchingDrive.Text : normalizedCurrentPath;
            }
            else
            {
                tabLocationHeader = currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();

                var rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(currentPath));
                if (rootItem)
                {
                    BaseStorageFolder currentFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(currentPath, rootItem));
                    if (currentFolder is not null && !string.IsNullOrEmpty(currentFolder.DisplayName))
                    {
                        tabLocationHeader = currentFolder.DisplayName;
                    }
                }
            }
        }

        return (tabLocationHeader, toolTipText);
    }

    private async Task UpdateToolbarIconInfoAsync()
    {
        ImageIconSource? iconSource = null;

        if (!string.IsNullOrEmpty(CurFolderPath))
        {
            iconSource = await GetToolbarIconInfoAsync(CurFolderPath);
        }

        if (iconSource is not null)
        {
            ImageSource = iconSource.ImageSource;
        }
    }

    private async Task<ImageIconSource?> GetToolbarIconInfoAsync(string currentPath)
    {
        // TODO: Fix bug when quitting app
        ImageIconSource iconSource = null!;
        try
        {
            iconSource = new ImageIconSource();
        }
        catch (Exception)
        {
            return null;
        }

        if (currentPath.Equals(Constants.UserEnvironmentPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
        {

        }
        else if (currentPath.Equals(Constants.UserEnvironmentPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
        {

        }
        else if (currentPath.Equals(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
        {
            // Use 48 for higher resolution, the other items look fine with 16.
            var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(currentPath, 48u, Windows.Storage.FileProperties.ThumbnailMode.ListView, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale, true);
            if (iconData is not null)
            {
                iconSource.ImageSource = await iconData.ToBitmapAsync();
            }
        }
        else if (currentPath.Equals(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
        {

        }
        else if (currentPath.Equals(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
        {

        }
        /*else if (App.LibraryManager.TryGetLibrary(currentPath, out LibraryLocationItem library))
        {

        }*/
        /*else if (WSLDistroManager.TryGetDistro(currentPath, out WslDistroItem? wslDistro) && currentPath.Equals(wslDistro.Path))
        {
            iconSource.ImageSource = new BitmapImage(wslDistro.Icon);
        }*/
        else
        {
            var normalizedCurrentPath = PathNormalization.NormalizePath(currentPath);
            var matchingCloudDrive = CloudDrivesManager.Drives.FirstOrDefault(x => normalizedCurrentPath.Equals(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
            if (matchingCloudDrive is not null)
            {
                iconSource.ImageSource = matchingCloudDrive.Icon;
            }
        }

        if (iconSource.ImageSource is null)
        {
            var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(currentPath, 16u, ThumbnailMode.ListView, ThumbnailOptions.UseCurrentScale, true);
            if (iconData is not null)
            {
                iconSource.ImageSource = await iconData.ToBitmapAsync();
            }
        }

        return iconSource;
    }

    private void SetLoadingIndicatorForTabs(bool isLoading)
    {
        if (isLoading)
        {
            ImageSource = null!;
            LoadingIndicatorStatus = true;
        }
        else
        {
            LoadingIndicatorStatus = false;
            // Make sure the ImageIconSource instance is created in UI thread
            RunOnDispatcherQueue(async () =>
            {
                await UpdateToolbarIconInfoAsync();
            });
        }
    }

    #endregion

    #region settings

    protected override void LoadSettings(FolderViewWidgetSettings settings)
    {
        var behaviour = RefreshBehaviours.None;

        if (ShowIconOverlay != settings.ShowIconOverlay)
        {
            ShowIconOverlay = settings.ShowIconOverlay;
            behaviour = RefreshBehaviours.RefreshItems;
        }

        if (ShowHiddenFile != settings.ShowHiddenFile)
        {
            ShowHiddenFile = settings.ShowHiddenFile;
            behaviour = RefreshBehaviours.RefreshItems;
        }

        if (AllowNavigation != settings.AllowNavigation)
        {
            AllowNavigation = settings.AllowNavigation;
            if (!AllowNavigation)
            {
                CanGoBack = false;
                CanNavigateToParent = false;
            }
            else
            {
                CanGoBack = navigationFolderPaths.Count > 0;

                var workingDir = FileSystemViewModel.WorkingDirectory ?? string.Empty;
                var pathRoot = PathNormalization.GetPathRoot(workingDir);
                var isRoot = workingDir == pathRoot;
                var isRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
                CanNavigateToParent = (!(string.IsNullOrEmpty(pathRoot) || isRoot || isRecycleBin)) && AllowNavigation;
            }
        }

        if (ShowExtension != settings.ShowExtension)
        {
            ShowExtension = settings.ShowExtension;
            behaviour = RefreshBehaviours.RefreshItems;
        }

        if (DeleteConfirmationPolicy != settings.DeleteConfirmationPolicy)
        {
            DeleteConfirmationPolicy = settings.DeleteConfirmationPolicy;
        }

        if (ShowThumbnail != settings.ShowThumbnail)
        {
            ShowThumbnail = settings.ShowThumbnail;
            behaviour = RefreshBehaviours.RefreshItems;
        }

        if (ConflictsResolveOption != settings.ConflictsResolveOption)
        {
            ConflictsResolveOption = settings.ConflictsResolveOption;
        }

        // Put this last so that it will navigate to the new path even if it needs to refresh items
        if (FolderPath != settings.FolderPath)
        {
            FolderPath = settings.FolderPath;
            navigationFolderPaths.Clear();
            behaviour = RefreshBehaviours.NavigateToPath;
        }

        refreshBehaviour = behaviour;
    }

    public override FolderViewWidgetSettings GetSettings()
    {
        return new FolderViewWidgetSettings()
        {
            FolderPath = CurFolderPath,
            ShowIconOverlay = ShowIconOverlay,
            ShowHiddenFile = ShowHiddenFile,
            AllowNavigation = AllowNavigation,
            ShowExtension = ShowExtension,
            DeleteConfirmationPolicy = DeleteConfirmationPolicy,
            ShowThumbnail = ShowThumbnail,
            ConflictsResolveOption = ConflictsResolveOption,
        };
    }

    #endregion

    #region path navigation

    private readonly Stack<string> navigationFolderPaths = new();

    public void Back_Click()
    {
        navigationFolderPaths.Pop();
        var path = navigationFolderPaths.Count == 0 ? FolderPath : navigationFolderPaths.Peek();
        NavigateWithArguments(new NavigationArguments()
        {
            NavPathParam = path,
            PushFolderPath = false,
            RefreshBehaviour = RefreshBehaviours.NavigateToPath
        });
    }

    public void Up_Click()
    {
        var workingDirectory = FileSystemViewModel.WorkingDirectory;
        if (workingDirectory is null || string.Equals(workingDirectory, PathNormalization.GetPathRoot(workingDirectory), StringComparison.OrdinalIgnoreCase))
        {
            //ParentShellPageInstance?.NavigateHome();
        }
        else
        {
            var path = PathNormalization.GetParentDir(workingDirectory);
            NavigateWithArguments(new NavigationArguments()
            {
                NavPathParam = path,
                PushFolderPath = true,
                RefreshBehaviour = RefreshBehaviours.NavigateToPath
            });
        }
    }

    public async Task Refresh_Click()
    {
        if (InstanceViewModel.IsPageTypeSearchResults)
        {
            /*ToolbarViewModel.CanRefresh = false;
            var searchInstance = new FolderSearch
            {
                Query = InstanceViewModel.CurrentSearchQuery ?? (string)TabItemParameter.NavigationParameter,
                Folder = FilesystemViewModel.WorkingDirectory,
                ThumbnailSize = InstanceViewModel.FolderSettings.GetIconSize(),
                SearchUnindexedItems = InstanceViewModel.SearchedUnindexedItems
            };

            await FilesystemViewModel.SearchAsync(searchInstance);*/
            await Task.CompletedTask;
        }
        else
        {
            ToolbarViewModel.CanRefresh = false;
            FileSystemViewModel?.RefreshItems(null);
        }
    }

    #endregion

    #region page navigation

    public void NavigateWithArguments(NavigationArguments navArgs)
    {
        _widgetManagerService.WidgetNavigateTo(WidgetWindow.WidgetType, WidgetWindow.IndexTag, navArgs);
    }

    #endregion

    #region item sources

    public IEnumerable<ListedItem>? GetAllItems()
    {
        var items = CollectionViewSource.IsSourceGrouped
            ? (CollectionViewSource.Source as BulkConcurrentObservableCollection<GroupedCollection<ListedItem>>)?.SelectMany(g => g) // add all items from each group to the new list
            : CollectionViewSource.Source as IEnumerable<ListedItem>;

        return items ?? new List<ListedItem>();
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            await Refresh_Click();
        }
        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        CommandsViewModel.Dispose();
        FileSystemViewModel.Dispose();
    }

    T IFolderViewViewModel.GetRequiredService<T>()
    {
        return typeof(T) switch
        {
            Type t when t == typeof(ICommandManager) => (T)CommandManager,
            Type t when t == typeof(IDialogService) => (T)DialogService,
            _ => null!,
        };
    }

    #endregion
}
