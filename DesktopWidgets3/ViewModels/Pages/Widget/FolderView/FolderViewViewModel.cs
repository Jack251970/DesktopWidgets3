using System.Text.RegularExpressions;
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
using Microsoft.UI.Xaml.Media.Imaging;
using Files.App.Data.EventArguments;
using Microsoft.UI.Xaml.Data;
using DesktopWidgets3.Contracts.Services;

namespace DesktopWidgets3.ViewModels.Pages.Widget;

public partial class FolderViewViewModel : BaseWidgetViewModel<FolderViewWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region view properties

    [ObservableProperty]
    public CollectionViewSource _collectionViewSource = new()
    {
        IsSourceGrouped = false,
    };

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

    private bool needRefresh = false;

    private string FolderPath { get; set; } = string.Empty;

    private bool ShowIconOverlay { get; set; } = true;

    private bool ShowHiddenFile { get; set; } = false;

    private bool ShowExtension { get; set; } = false;

    #endregion

    #region current path

    public string CurFolderPath => ItemViewModel.WorkingDirectory;

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

    #region models from Files

    public ItemViewModel ItemViewModel;

    public CurrentInstanceViewModel InstanceViewModel;

    public BaseLayoutViewModel CommandsViewModel;

    public ItemManipulationModel ItemManipulationModel;

    public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel;

    public ICommandManager CommandManager => _commandManager;

    public IFileSystemHelpers FileSystemHelpers => _fileSystemHelpers;

    public LayoutPreferencesManager FolderSettings => InstanceViewModel.FolderSettings;

    public FolderViewViewModel ToolbarViewModel => this;

    #endregion

    private readonly ICommandManager _commandManager;
    private readonly IFileSystemHelpers _fileSystemHelpers;
    private readonly IWidgetManagerService _widgetManagerService;

    public FolderViewViewModel(ICommandManager commandManager, IFileSystemHelpers fileSystemHelpers, IWidgetManagerService widgetManagerService)
    {
        _commandManager = commandManager;
        _commandManager.Initialize(this);
        _fileSystemHelpers = fileSystemHelpers;
        _widgetManagerService = widgetManagerService;

        InstanceViewModel = new();
        CommandsViewModel = new();
        ItemManipulationModel = new();
        SelectedItemsPropertiesViewModel = new();
        ItemViewModel = new(this, FolderSettings);

        NavigatedTo += FolderViewViewModel_NavigatedTo;
    }

    #region refresh items

    private async void FolderViewViewModel_NavigatedTo(object? sender, object e)
    {
        if (e is FolderViewWidgetSettings settings && needRefresh)
        {
            needRefresh = false;

            // Prepare navigation arguments
            e = new NavigationArguments()
            {
                FocusOnNavigation = true,
                NavPathParam = settings.FolderPath,
                PushFolderPath = false,
            };
        }

        if (e is NavigationArguments navigationArguments)
        {
            // Git properties are not loaded by default
            //ItemViewModel.EnabledGitProperties = GitProperties.None;

            //InitializeCommandsViewModel();

            IsItemSelected = false;

            /*FolderSettings!.LayoutModeChangeRequested += BaseFolderSettings_LayoutModeChangeRequested;
            FolderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
            FolderSettings.GroupDirectionPreferenceUpdated += FolderSettings_GroupDirectionPreferenceUpdated;
            FolderSettings.GroupByDateUnitPreferenceUpdated += FolderSettings_GroupByDateUnitPreferenceUpdated;*/

            /*ItemViewModel.EmptyTextType = EmptyTextType.None;
            ToolbarViewModel.CanRefresh = true;*/

            if (!navigationArguments.IsSearchResultPage)
            {
                var previousDir = ItemViewModel.WorkingDirectory;
                await ItemViewModel.SetWorkingDirectoryAsync(navigationArguments.NavPathParam);

                // pathRoot will be empty on recycle bin path
                var workingDir = ItemViewModel.WorkingDirectory ?? string.Empty;
                var pathRoot = PathNormalization.GetPathRoot(workingDir);

                var isRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
                InstanceViewModel.IsPageTypeRecycleBin = isRecycleBin;

                // Can't go up from recycle bin
                ToolbarViewModel.IsNavigateUpExecutable = !(string.IsNullOrEmpty(pathRoot) || isRecycleBin);

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
                    await ItemViewModel.RefreshItems(previousDir);//, SetSelectedItemsOnNavigation);
                }
                /*else
                {
                    ToolbarViewModel.CanGoForward = false;
                }*/
            }
            else
            {
                await ItemViewModel.SetWorkingDirectoryAsync(navigationArguments.SearchPathParam);

                //ToolbarViewModel.CanGoForward = false;
                ToolbarViewModel.IsNavigateUpExecutable = false;

                var workingDir = ItemViewModel.WorkingDirectory ?? string.Empty;

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
            ToolbarViewModel.IsNavigateBackExecutable = navigationFolderPaths.Count > 0;

            await RefreshToolbar();
        }
    }

    private void UpdateCollectionViewSource()
    {
        var items = ItemViewModel.FilesAndFolders;
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

    private async Task RefreshToolbar()
    {
        if (DiskRegex().IsMatch(CurFolderPath))
        {
            FolderName = CurFolderPath[..1];
        }
        else
        {
            FolderName = Path.GetFileName(CurFolderPath);
        }

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

    [GeneratedRegex("^[a-zA-Z]:\\\\$")]
    private static partial Regex DiskRegex();

    #endregion

    #region settings

    protected override void LoadSettings(FolderViewWidgetSettings settings)
    {
        needRefresh = false;

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

        if (ShowExtension != settings.ShowExtension)
        {
            ShowExtension = settings.ShowExtension;
            needRefresh = true;
        }

        if (FolderPath != settings.FolderPath)
        {
            FolderPath = settings.FolderPath;
            navigationFolderPaths.Clear();
            needRefresh = true;
        }

        if (AllowNavigation != settings.AllowNavigation)
        {
            AllowNavigation = settings.AllowNavigation;
        }
    }

    public override FolderViewWidgetSettings GetSettings()
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

    #region path navigation

    private readonly Stack<string> navigationFolderPaths = new();

    public void NavigateBack()
    {
        navigationFolderPaths.Pop();
        var path = navigationFolderPaths.Count == 0 ? FolderPath : navigationFolderPaths.Peek();
        NavigateWithArguments(new NavigationArguments()
        {
            NavPathParam = path,
            PushFolderPath = false
        });
    }

    public void NavigateUp()
    {
        var workingDirectory = ItemViewModel.WorkingDirectory;
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
                PushFolderPath = true
            });
        }
    }

    public void NavigateRefresh()
    {
        var path = ItemViewModel.WorkingDirectory;
        NavigateWithArguments(new NavigationArguments()
        {
            NavPathParam = path,
            PushFolderPath = false
        });
    }

    #endregion

    #region page navigation

    public void NavigateWithArguments(NavigationArguments navArgs)
    {
        _widgetManagerService.WidgetNavigateTo(WidgetWindow.WidgetType, WidgetWindow.IndexTag, navArgs);
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            NavigateRefresh();
        }
        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        CommandsViewModel.Dispose();
        ItemViewModel.Dispose();
    }

    #endregion
}
