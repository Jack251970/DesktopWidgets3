using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.ViewModels.Settings;
using Files.App.Data.Enums;

namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public partial class FolderViewSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region commands

    public ClickCommand SelectFolderPathCommand
    {
        get;
    }

    #endregion

    #region view properties

    [ObservableProperty]
    private string _folderPath = $"C:\\";

    [ObservableProperty]
    private bool _allowNavigation = true;

    [ObservableProperty]
    private bool _moveShellExtensionsToSubMenu = true;

    [ObservableProperty]
    private bool _syncFolderPreferencesAcrossDirectories;

    [ObservableProperty]
    private bool _showHiddenItems;

    [ObservableProperty]
    private bool _showDotFiles;

    [ObservableProperty]
    private bool _showProtectedSystemFiles;

    [ObservableProperty]
    private bool _areAlternateStreamsVisible;

    [ObservableProperty]
    private bool _showFileExtensions = false;

    [ObservableProperty]
    private bool _showThumbnails = true;

    [ObservableProperty]
    private bool _showCheckboxesWhenSelectingItems = true;

    [ObservableProperty]
    private int _selectedDeleteConfirmationPolicyIndex;

    [ObservableProperty]
    private bool _showFileExtensionWarning = true;

    public TagsViewModel TagsViewModel { get; } = new();

    public AboutViewModel AboutViewModel { get; } = new();

    #endregion

    private FolderViewWidgetSettings Settings => (FolderViewWidgetSettings)WidgetSettings!;

    public FolderViewSettingsViewModel()
    {
        SelectFolderPathCommand = new ClickCommand(SelectFoldePath);
    }

    protected override WidgetType InitializeWidgetType() => WidgetType.FolderView;

    protected override void InitializeWidgetSettings()
    {
        FolderPath = Settings.FolderPath;
        AllowNavigation = Settings.AllowNavigation;
        MoveShellExtensionsToSubMenu = Settings.MoveShellExtensionsToSubMenu;
        SyncFolderPreferencesAcrossDirectories = Settings.SyncFolderPreferencesAcrossDirectories;
        ShowHiddenItems = Settings.ShowHiddenItems;
        ShowDotFiles = Settings.ShowDotFiles;
        ShowProtectedSystemFiles = Settings.ShowProtectedSystemFiles;
        AreAlternateStreamsVisible = Settings.AreAlternateStreamsVisible;
        ShowFileExtensions = Settings.ShowFileExtensions;
        ShowThumbnails = Settings.ShowThumbnails;
        ShowCheckboxesWhenSelectingItems = Settings.ShowCheckboxesWhenSelectingItems;
        SelectedDeleteConfirmationPolicyIndex = (int)Settings.DeleteConfirmationPolicy;
        ShowFileExtensionWarning = Settings.ShowFileExtensionWarning;
    }

    private async void SelectFoldePath()
    {
        if (IsInitialized)
        {
            var newPath = await StorageHelper.PickSingleFolderDialog(App.MainWindow.WindowHandle);
            if (!string.IsNullOrEmpty(newPath))
            {
                Settings.FolderPath = FolderPath = newPath;
                NeedUpdate = true;
            }
        }
    }

    partial void OnAllowNavigationChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.AllowNavigation = value;
            NeedUpdate = true;
        }
    }

    partial void OnMoveShellExtensionsToSubMenuChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.MoveShellExtensionsToSubMenu = value;
            NeedUpdate = true;
        }
    }

    partial void OnSyncFolderPreferencesAcrossDirectoriesChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.SyncFolderPreferencesAcrossDirectories = value;
            NeedUpdate = true;
        }
    }

    partial void OnShowHiddenItemsChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.ShowHiddenItems = value;
            NeedUpdate = true;
        }
    }

    partial void OnShowDotFilesChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.ShowDotFiles = value;
            NeedUpdate = true;
        }
    }

    partial void OnShowProtectedSystemFilesChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.ShowProtectedSystemFiles = value;
            NeedUpdate = true;
        }
    }

    partial void OnAreAlternateStreamsVisibleChanged(bool value)
    {
        if (IsInitialized)
        { 
            Settings.AreAlternateStreamsVisible = value;
            NeedUpdate = true;
        }
    }

    partial void OnShowFileExtensionsChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.ShowFileExtensions = value;
            NeedUpdate = true;
        }
    }

    partial void OnShowThumbnailsChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.ShowThumbnails = value;
            NeedUpdate = true;
        }
    }

    partial void OnShowCheckboxesWhenSelectingItemsChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.ShowCheckboxesWhenSelectingItems = value;
            NeedUpdate = true;
        }
    }

    partial void OnSelectedDeleteConfirmationPolicyIndexChanged(int value)
    {
        if (IsInitialized)
        {
            Settings.DeleteConfirmationPolicy = (DeleteConfirmationPolicies)value;
            NeedUpdate = true;
        }
    }

    partial void OnShowFileExtensionWarningChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.ShowFileExtensionWarning = value;
            NeedUpdate = true;
        }
    }
}
