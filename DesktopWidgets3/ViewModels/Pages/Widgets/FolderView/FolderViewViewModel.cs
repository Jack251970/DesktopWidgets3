using Files.App.Views;
using Files.App.ViewModels;
using Files.App.Data.EventArguments;
using Files.App.Data.Enums;
using Files.App.Data.Contracts;
using Windows.Foundation;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using FilesApp = Files.App.App;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class FolderViewViewModel : BaseWidgetViewModel<FolderViewWidgetSettings>, IWidgetUpdate, IWidgetClose, IFolderViewViewModel
{
    private readonly IWidgetManagerService _widgetManagerService;

    private string FolderPath = string.Empty;

    private bool AllowNavigation = true;

    private readonly FilesApp App;

    #region interfaces

    bool IFolderViewViewModel.AllowNavigation => AllowNavigation;

    public event Action<string>? FolderPathChanged;

    WindowEx IFolderViewViewModel.MainWindow => WidgetWindow;

    IntPtr IFolderViewViewModel.WindowHandle => WidgetWindow.WindowHandle;

    AppWindow IFolderViewViewModel.AppWindow => WidgetWindow.AppWindow;

    Rect IFolderViewViewModel.Bounds => WidgetWindow.Bounds;

    Page IFolderViewViewModel.Page => WidgetPage;

    UIElement IFolderViewViewModel.Content
    {
        get => WidgetPage.Content;
        set => WidgetPage.Content = value;
    }

    XamlRoot IFolderViewViewModel.XamlRoot => WidgetPage.Content.XamlRoot;

    TaskCompletionSource? IFolderViewViewModel.SplashScreenLoadingTCS => App.SplashScreenLoadingTCS;

    CommandBarFlyout? IFolderViewViewModel.LastOpenedFlyout 
    {
        set => App.LastOpenedFlyout = value;
    }

    event PropertyChangedEventHandler? IFolderViewViewModel.PropertyChanged
    {
        add => PropertyChanged += value;
        remove => PropertyChanged -= value;
    }

    private int tabStripSelectedIndex = -1;
    int IFolderViewViewModel.TabStripSelectedIndex
    {
        get => tabStripSelectedIndex;
        set
        {
            SetProperty(ref tabStripSelectedIndex, value);

            if (value >= 0 && value < MainPageViewModel.AppInstancesManager.Get(this).Count)
            {
                var rootFrame = (Frame)WidgetPage.Content;
                var mainView = (MainPage)rootFrame.Content;
                mainView.ViewModel.SelectedTabItem = MainPageViewModel.AppInstancesManager.Get(this)[value];
            }
        }
    }

    private bool canShowDialog = true;
    public bool CanShowDialog
    {
        get => canShowDialog;
        set => SetProperty(ref canShowDialog, value);
    }

    #endregion

    public FolderViewViewModel(IWidgetManagerService widgetManagerService)
    {
        _widgetManagerService = widgetManagerService;

        NavigatedTo += FolderViewViewModel_NavigatedTo;
        
        // Initialize files app to handle files lifecycle
        App = new(this);
    }
    
    #region initialization

    public Page WidgetPage { get; set; } = null!;

    private async void FolderViewViewModel_NavigatedTo(object? parameter)
    {
        if (_isInitialized)
        {
            var userSettingsService = App.GetRequiredService<IUserSettingsService>();

            // Don't leave the app running in background
            userSettingsService.GeneralSettingsService.LeaveAppRunning = false;

            // Don't show background running notification
            userSettingsService.AppSettingsService.ShowBackgroundRunningNotification = false;

            // Show favorites section in sidebar
            userSettingsService.GeneralSettingsService.ShowPinnedSection = true;

            // Load StatusCenterTeachingTip settings
            userSettingsService.AppSettingsService.ShowStatusCenterTeachingTip = await LocalSettingsExtensions.ReadLocalSettingAsync("ShowStatusCenterTeachingTip", true);

            // Register callback of user settings service after setting initialization
            userSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

            App.OnLaunched(FolderPath);
        }
    }

    #endregion

    #region settings

    protected override void LoadSettings(FolderViewWidgetSettings settings)
    {
        var _userSettingsService = App.GetRequiredService<IUserSettingsService>();

        if (AllowNavigation != settings.AllowNavigation)
        {
            AllowNavigation = settings.AllowNavigation;
        }

        if (_userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu != settings.MoveShellExtensionsToSubMenu)
        {
            _userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu = settings.MoveShellExtensionsToSubMenu;
        }

        if (_userSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories != settings.SyncFolderPreferencesAcrossDirectories)
        {
            _userSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories = settings.SyncFolderPreferencesAcrossDirectories;
        }

        if (_userSettingsService.LayoutSettingsService.DetailsViewSize != settings.DetailsViewSize)
        {
            _userSettingsService.LayoutSettingsService.DetailsViewSize = settings.DetailsViewSize;
        }

        if (_userSettingsService.LayoutSettingsService.ListViewSize != settings.ListViewSize)
        {
            _userSettingsService.LayoutSettingsService.ListViewSize = settings.ListViewSize;
        }

        if (_userSettingsService.LayoutSettingsService.TilesViewSize != settings.TilesViewSize)
        {
            _userSettingsService.LayoutSettingsService.TilesViewSize = settings.TilesViewSize;
        }

        if (_userSettingsService.LayoutSettingsService.GridViewSize != settings.GridViewSize)
        {
            _userSettingsService.LayoutSettingsService.GridViewSize = settings.GridViewSize;
        }

        if (_userSettingsService.LayoutSettingsService.ColumnsViewSize != settings.ColumnsViewSize)
        {
            _userSettingsService.LayoutSettingsService.ColumnsViewSize = settings.ColumnsViewSize;
        }

        if (_userSettingsService.FoldersSettingsService.ShowHiddenItems != settings.ShowHiddenItems)
        {
            _userSettingsService.FoldersSettingsService.ShowHiddenItems = settings.ShowHiddenItems;
        }

        if (_userSettingsService.FoldersSettingsService.ShowDotFiles != settings.ShowDotFiles)
        {
            _userSettingsService.FoldersSettingsService.ShowDotFiles = settings.ShowDotFiles;
        }

        if (_userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles != settings.ShowProtectedSystemFiles)
        {
            _userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles = settings.ShowProtectedSystemFiles;
        }

        if (_userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible != settings.AreAlternateStreamsVisible)
        {
            _userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible = settings.AreAlternateStreamsVisible;
        }

        if (_userSettingsService.FoldersSettingsService.ShowFileExtensions != settings.ShowFileExtensions)
        {
            _userSettingsService.FoldersSettingsService.ShowFileExtensions = settings.ShowFileExtensions;
        }

        if (_userSettingsService.FoldersSettingsService.ShowThumbnails != settings.ShowThumbnails)
        {
            _userSettingsService.FoldersSettingsService.ShowThumbnails = settings.ShowThumbnails;
        }

        if (_userSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems != settings.ShowCheckboxesWhenSelectingItems)
        {
            _userSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems = settings.ShowCheckboxesWhenSelectingItems;
        }

        if (_userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy != settings.DeleteConfirmationPolicy)
        {
            _userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy = settings.DeleteConfirmationPolicy;
        }

        if (_userSettingsService.FoldersSettingsService.ShowFileExtensionWarning != settings.ShowFileExtensionWarning)
        {
            _userSettingsService.FoldersSettingsService.ShowFileExtensionWarning = settings.ShowFileExtensionWarning;
        }

        if (_userSettingsService.GeneralSettingsService.ConflictsResolveOption != settings.ConflictsResolveOption)
        {
            _userSettingsService.GeneralSettingsService.ConflictsResolveOption = settings.ConflictsResolveOption;
        }

        if (_userSettingsService.ApplicationSettingsService.ShowRunningAsAdminPrompt != settings.ShowRunningAsAdminPrompt)
        {
            _userSettingsService.ApplicationSettingsService.ShowRunningAsAdminPrompt = settings.ShowRunningAsAdminPrompt;
        }

        if (FolderPath != settings.FolderPath)
        {
            FolderPath = settings.FolderPath;
            if (_isInitialized)
            {
                FolderPathChanged?.Invoke(FolderPath);
            }
        }
    }

    public override FolderViewWidgetSettings GetSettings()
    {
        var _userSettingsService = App.GetRequiredService<IUserSettingsService>();

        return new FolderViewWidgetSettings()
        {
            FolderPath = FolderPath,
            AllowNavigation = AllowNavigation,
            MoveShellExtensionsToSubMenu = _userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu,
            SyncFolderPreferencesAcrossDirectories = _userSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories,
            DetailsViewSize = _userSettingsService.LayoutSettingsService.DetailsViewSize,
            ListViewSize = _userSettingsService.LayoutSettingsService.ListViewSize,
            TilesViewSize = _userSettingsService.LayoutSettingsService.TilesViewSize,
            GridViewSize = _userSettingsService.LayoutSettingsService.GridViewSize,
            ColumnsViewSize = _userSettingsService.LayoutSettingsService.ColumnsViewSize,
            ShowHiddenItems = _userSettingsService.FoldersSettingsService.ShowHiddenItems,
            ShowDotFiles = _userSettingsService.FoldersSettingsService.ShowDotFiles,
            ShowProtectedSystemFiles = _userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles,
            AreAlternateStreamsVisible = _userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible,
            ShowFileExtensions = _userSettingsService.FoldersSettingsService.ShowFileExtensions,
            ShowThumbnails = _userSettingsService.FoldersSettingsService.ShowThumbnails,
            ShowCheckboxesWhenSelectingItems = _userSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems,
            DeleteConfirmationPolicy = _userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy,
            ShowFileExtensionWarning = _userSettingsService.FoldersSettingsService.ShowFileExtensionWarning,
            ConflictsResolveOption = _userSettingsService.GeneralSettingsService.ConflictsResolveOption,
            ShowRunningAsAdminPrompt = _userSettingsService.ApplicationSettingsService.ShowRunningAsAdminPrompt,
        };
    }

    private async void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
    {
        var Settings = GetSettings();

        switch (e.SettingName)
		{
            case nameof(IGeneralSettingsService.MoveShellExtensionsToSubMenu):
                Settings.MoveShellExtensionsToSubMenu = (bool)e.NewValue!;
                break;
            case nameof(ILayoutSettingsService.SyncFolderPreferencesAcrossDirectories):
                Settings.SyncFolderPreferencesAcrossDirectories = (bool)e.NewValue!;
                break;
            case nameof(ILayoutSettingsService.DetailsViewSize):
                Settings.DetailsViewSize = (DetailsViewSizeKind)Enum.Parse(typeof(DetailsViewSizeKind), e.NewValue!.ToString()!);
                break;
            case nameof(ILayoutSettingsService.ListViewSize):
                Settings.ListViewSize = (ListViewSizeKind)Enum.Parse(typeof(ListViewSizeKind), e.NewValue!.ToString()!);
                break;
            case nameof(ILayoutSettingsService.TilesViewSize):
                Settings.TilesViewSize = (TilesViewSizeKind)Enum.Parse(typeof(TilesViewSizeKind), e.NewValue!.ToString()!);
                break;
            case nameof(ILayoutSettingsService.GridViewSize):
                Settings.GridViewSize = (GridViewSizeKind)Enum.Parse(typeof(GridViewSizeKind), e.NewValue!.ToString()!);
                break;
            case nameof(ILayoutSettingsService.ColumnsViewSize):
                Settings.ColumnsViewSize = (ColumnsViewSizeKind)Enum.Parse(typeof(ColumnsViewSizeKind), e.NewValue!.ToString()!);
                break;
			case nameof(IFoldersSettingsService.ShowHiddenItems):
                Settings.ShowHiddenItems = (bool)e.NewValue!;
                break;
            case nameof(IFoldersSettingsService.ShowDotFiles):
                Settings.ShowDotFiles = (bool)e.NewValue!;
                break;
            case nameof(IFoldersSettingsService.ShowProtectedSystemFiles):
                Settings.ShowProtectedSystemFiles = (bool)e.NewValue!;
                break;
            case nameof(IFoldersSettingsService.AreAlternateStreamsVisible):
                Settings.AreAlternateStreamsVisible = (bool)e.NewValue!;
                break;
            case nameof(IFoldersSettingsService.ShowFileExtensions):
                Settings.ShowFileExtensions = (bool)e.NewValue!;
                break;
            case nameof(IFoldersSettingsService.ShowThumbnails):
                Settings.ShowThumbnails = (bool)e.NewValue!;
                break;
            case nameof(IFoldersSettingsService.DeleteConfirmationPolicy):
                Settings.DeleteConfirmationPolicy = (DeleteConfirmationPolicies)Enum.Parse(typeof(DeleteConfirmationPolicies), e.NewValue!.ToString()!);
                break;
            case nameof(IFoldersSettingsService.ShowFileExtensionWarning):
                Settings.ShowFileExtensionWarning = (bool)e.NewValue!;
                break;
            case nameof(IGeneralSettingsService.ConflictsResolveOption):
                Settings.ConflictsResolveOption = (FileNameConflictResolveOptionType)Enum.Parse(typeof(FileNameConflictResolveOptionType), e.NewValue!.ToString()!);
                break;
            case nameof(IApplicationSettingsService.ShowRunningAsAdminPrompt):
                Settings.ShowRunningAsAdminPrompt = (bool)e.NewValue!;
                break;
            case nameof(Files.App.Data.Contracts.IAppSettingsService.ShowStatusCenterTeachingTip):
                await LocalSettingsExtensions.SaveLocalSettingAsync("ShowStatusCenterTeachingTip", (bool)e.NewValue!);
                return;
            default:
                return;
		}

        await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        NavigatedTo -= FolderViewViewModel_NavigatedTo;
    }

    T IFolderViewViewModel.GetRequiredService<T>() where T : class => App.GetRequiredService<T>();

    #endregion
}
