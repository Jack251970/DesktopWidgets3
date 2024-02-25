using CommunityToolkit.Mvvm.ComponentModel;

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
    private bool _showHiddenFile;

    [ObservableProperty]
    private bool _allowNavigation = true;

    [ObservableProperty]
    private bool _showExtension = false;

    [ObservableProperty]
    private bool _showThumbnail = true;

    [ObservableProperty]
    private bool _moveShellExtensionsToSubMenu = true;

    #endregion

    private readonly IWidgetManagerService _widgetManagerService;

    private FolderViewWidgetSettings Settings => (FolderViewWidgetSettings)WidgetSettings!;

    public FolderViewSettingsViewModel(IWidgetManagerService widgetManagerService)
    {
        _widgetManagerService = widgetManagerService;

        SelectFolderPathCommand = new ClickCommand(SelectFoldePath);
    }

    protected override WidgetType InitializeWidgetType() => WidgetType.FolderView;

    protected override void InitializeWidgetSettings()
    {
        FolderPath = Settings.FolderPath;
        AllowNavigation = Settings.AllowNavigation;
        MoveShellExtensionsToSubMenu = Settings.MoveShellExtensionsToSubMenu;
        ShowHiddenFile = Settings.ShowHiddenFile;
        ShowExtension = Settings.ShowExtension;
        ShowThumbnail = Settings.ShowThumbnail;
    }

    private async void SelectFoldePath()
    {
        if (IsInitialized)
        {
            var newPath = await StorageHelper.PickSingleFolderDialog(App.MainWindow.WindowHandle);
            if (!string.IsNullOrEmpty(newPath))
            {
                Settings.FolderPath = FolderPath = newPath;
                await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
            }
        }
    }

    partial void OnAllowNavigationChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.AllowNavigation = value;
            _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
        }
    }

    partial void OnMoveShellExtensionsToSubMenuChanged(bool value)
    {
        if (IsInitialized && !IsEnabled)
        {
            Settings.MoveShellExtensionsToSubMenu = value;
            _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
        }
    }

    partial void OnShowHiddenFileChanged(bool value)
    {
        if (IsInitialized && !IsEnabled)
        {
            Settings.ShowHiddenFile = value;
            _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
        }
    }

    partial void OnShowExtensionChanged(bool value)
    {
        if (IsInitialized && !IsEnabled)
        {
            Settings.ShowExtension = value;
            _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
        }
    }

    partial void OnShowThumbnailChanged(bool value)
    {
        if (IsInitialized && !IsEnabled)
        {
            Settings.ShowThumbnail = value;
            _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
        }
    }
}
