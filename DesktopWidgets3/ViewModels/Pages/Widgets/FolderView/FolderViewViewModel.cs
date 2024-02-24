﻿using Files.App.Views;
using Files.App.ViewModels;
using Files.Core.Data.EventArguments;
using Files.Core.Data.Enums;
using Files.Core.Services.Settings;

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

    Page IFolderViewViewModel.Page => WidgetPage;

    UIElement IFolderViewViewModel.MainWindowContent => WidgetPage.Content;

    XamlRoot IFolderViewViewModel.XamlRoot => WidgetPage.Content.XamlRoot;

    TaskCompletionSource? IFolderViewViewModel.SplashScreenLoadingTCS => App.SplashScreenLoadingTCS;

    CommandBarFlyout? IFolderViewViewModel.LastOpenedFlyout 
    {
        get => App.LastOpenedFlyout;
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

            if (value >= 0 && value < MainPageViewModel.AppInstances[this].Count)
            {
                var rootFrame = (Frame)WidgetPage.Content;
                var mainView = (MainPage)rootFrame.Content;
                mainView.ViewModel.SelectedTabItem = MainPageViewModel.AppInstances[this][value];
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

    private void FolderViewViewModel_NavigatedTo(object? parameter, bool isInitialized)
    {
        if (!isInitialized)
        {
            // All callback of user settings service after setting initialization
            App.GetService<IUserSettingsService>().OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

            App.OnLaunched(FolderPath);
        }
    }

    #endregion

    #region settings

    protected override void LoadSettings(FolderViewWidgetSettings settings)
    {
        var _userSettingsService = App.GetService<IUserSettingsService>();

        if (AllowNavigation != settings.AllowNavigation)
        {
            AllowNavigation = settings.AllowNavigation;
        }

        if (_userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu != settings.MoveShellExtensionsToSubMenu)
        {
            _userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu = settings.MoveShellExtensionsToSubMenu;
        }

        if (_userSettingsService.FoldersSettingsService.ShowHiddenItems != settings.ShowHiddenFile)
        {
            _userSettingsService.FoldersSettingsService.ShowHiddenItems = settings.ShowHiddenFile;
        }

        if (_userSettingsService.FoldersSettingsService.ShowFileExtensions != settings.ShowExtension)
        {
            _userSettingsService.FoldersSettingsService.ShowFileExtensions = settings.ShowExtension;
        }

        if (_userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy != settings.DeleteConfirmationPolicy)
        {
            _userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy = settings.DeleteConfirmationPolicy;
        }

        if (_userSettingsService.FoldersSettingsService.ShowThumbnails != settings.ShowThumbnail)
        {
            _userSettingsService.FoldersSettingsService.ShowThumbnails = settings.ShowThumbnail;
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
            FolderPathChanged?.Invoke(FolderPath);
        }
    }

    public override FolderViewWidgetSettings GetSettings()
    {
        var _userSettingsService = App.GetService<IUserSettingsService>();
        return new FolderViewWidgetSettings()
        {
            FolderPath = FolderPath,
            AllowNavigation = AllowNavigation,
            MoveShellExtensionsToSubMenu = _userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu,
            ShowHiddenFile = _userSettingsService.FoldersSettingsService.ShowHiddenItems,
            ShowExtension = _userSettingsService.FoldersSettingsService.ShowFileExtensions,
            ShowThumbnail = _userSettingsService.FoldersSettingsService.ShowThumbnails,
            DeleteConfirmationPolicy = _userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy,
            ConflictsResolveOption = _userSettingsService.GeneralSettingsService.ConflictsResolveOption,
            ShowRunningAsAdminPrompt = _userSettingsService.ApplicationSettingsService.ShowRunningAsAdminPrompt,
        };
    }

    private async void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
    {
        var WidgetType = WidgetWindow.WidgetType;
        var IndexTag = WidgetWindow.IndexTag;
        var Settings = GetSettings();

        switch (e.SettingName)
		{
            case nameof(IGeneralSettingsService.MoveShellExtensionsToSubMenu):
                Settings.MoveShellExtensionsToSubMenu = (bool)e.NewValue!;
                await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
                break;
			case nameof(IFoldersSettingsService.ShowHiddenItems):
                Settings.ShowHiddenFile = (bool)e.NewValue!;
                await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
                break;
            case nameof(IFoldersSettingsService.ShowFileExtensions):
                Settings.ShowExtension = (bool)e.NewValue!;
                await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
                break;
            case nameof(IFoldersSettingsService.ShowThumbnails):
                Settings.ShowThumbnail = (bool)e.NewValue!;
                await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
                break;
            case nameof(IFoldersSettingsService.DeleteConfirmationPolicy):
                Settings.DeleteConfirmationPolicy = (DeleteConfirmationPolicies)e.NewValue!;
                await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
                break;
            case nameof(IGeneralSettingsService.ConflictsResolveOption):
                Settings.ConflictsResolveOption = (FileNameConflictResolveOptionType)e.NewValue!;
                await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
                break;
            case nameof(IApplicationSettingsService.ShowRunningAsAdminPrompt):
                Settings.ShowRunningAsAdminPrompt = (bool)e.NewValue!;
                await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, Settings);
                break;
		}
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

    T IFolderViewViewModel.GetService<T>() where T : class => App.GetService<T>();

    #endregion
}
