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
using DesktopWidgets3.Views.Windows;
using Files.Core.Data.Enums;
using Files.Core.Services;
using static Files.App.Data.Models.ItemViewModel;
using static Files.App.Data.EventArguments.NavigationArguments;

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
    private bool _canGoBack = false;

    [ObservableProperty]
    private bool _canNavigateToParent = false;

    [ObservableProperty]
    private bool _canRefresh = true;

    [ObservableProperty]
    private BitmapImage? _folderPathIcon = null;

    [ObservableProperty]
    private BitmapImage? _folderPathIconOverlay = null;

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

    #region dialog

    [ObservableProperty]
    public static bool _canShowDialog = true;

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

    public IDialogService DialogService => _dialogService;

    #endregion

    private readonly ICommandManager _commandManager;
    private readonly IDialogService _dialogService;
    private readonly IFileSystemHelpers _fileSystemHelpers;
    private readonly IWidgetManagerService _widgetManagerService;

    public FolderViewViewModel(ICommandManager commandManager, IDialogService dialogService, IFileSystemHelpers fileSystemHelpers, IWidgetManagerService widgetManagerService)
    {
        _commandManager = commandManager;
        _commandManager.Initialize(this);
        _dialogService = dialogService;
        _dialogService.Initialize(this);
        _fileSystemHelpers = fileSystemHelpers;
        _widgetManagerService = widgetManagerService;

        NavigatedTo += FolderViewViewModel_NavigatedTo;

        InstanceViewModel = new();
        CommandsViewModel = new();
        ItemManipulationModel = new();
        SelectedItemsPropertiesViewModel = new();
        ItemViewModel = new(this, FolderSettings);

        ItemViewModel.ItemLoadStatusChanged += ItemViewModel_ItemLoadStatusChanged;
    }

    #region refresh items

    private async void FolderViewViewModel_NavigatedTo(object? sender, object e)
    {
        if (e is FolderViewWidgetSettings settings)
        {
            // Prepare navigation arguments
            e = new NavigationArguments()
            {
                NavPathParam = settings.FolderPath,
                PushFolderPath = false,
                RefreshBehaviour = refreshBehaviour
            };
        }

        if (e is NavigationArguments navigationArguments)
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
                    var previousDir = ItemViewModel.WorkingDirectory;
                    await ItemViewModel.SetWorkingDirectoryAsync(navigationArguments.NavPathParam);

                    // pathRoot will be empty on recycle bin path
                    var workingDir = ItemViewModel.WorkingDirectory ?? string.Empty;
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
                    ToolbarViewModel.CanNavigateToParent = false;

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
                ToolbarViewModel.CanGoBack = navigationFolderPaths.Count > 0 && AllowNavigation;

                await RefreshToolbar();
            }
            else if (navigationArguments.RefreshBehaviour == RefreshBehaviours.RefreshItems)
            {
                await ItemViewModel.RefreshItems(null);
            }
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

    public async Task RefreshIfNoWatcherExistsAsync()
    {
        if (ItemViewModel.HasNoWatcher)
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

                    var itemToSelect = ItemViewModel.FilesAndFolders.Where((item) => item.ItemPath == folderToSelect).FirstOrDefault();

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

    #region refresh toolbar

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

    [GeneratedRegex("^[a-zA-Z]:\\\\$")]
    private static partial Regex DiskRegex();

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

    private void SetLoadingIndicatorForTabs(bool isLoading)
    {
        // TODO: handle loading indicator here
        /*var multitaskingControls = ((MainWindow.Instance.Content as Frame).Content as MainPage).ViewModel.MultitaskingControls;

        foreach (var x in multitaskingControls)
            x.SetLoadingIndicatorStatus(x.Items.FirstOrDefault(x => x.TabItemContent == PaneHolder), isLoading);*/
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

                var workingDir = ItemViewModel.WorkingDirectory ?? string.Empty;
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
            ItemViewModel?.RefreshItems(null);
        }
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
            await Refresh_Click();
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
