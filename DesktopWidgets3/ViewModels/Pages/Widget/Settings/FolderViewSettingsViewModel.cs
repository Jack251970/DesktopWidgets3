﻿using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.ViewModels.Commands;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

public partial class FolderViewSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region commands

    public ClickCommand SelectFolderPathCommand
    {
        get;
    }

    #endregion

    #region observable properties

    [ObservableProperty]
    private string _folderPath = $"C:\\";

    [ObservableProperty]
    private bool _iconOverlay = true;

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
        IconOverlay = Settings.ShowIconOverlay;
    }

    private async void SelectFoldePath()
    {
        if (_isInitialized)
        {
            var newPath = await SelectFolderDialogHelper.PickSingleFolderDialog();
            if (!string.IsNullOrEmpty(newPath))
            {
                Settings.FolderPath = FolderPath = newPath;
                await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
            }
        }
    }

    partial void OnIconOverlayChanged(bool value)
    {
        if (_isInitialized)
        {
            Settings.ShowIconOverlay = value;
            _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
        }
    }
}
