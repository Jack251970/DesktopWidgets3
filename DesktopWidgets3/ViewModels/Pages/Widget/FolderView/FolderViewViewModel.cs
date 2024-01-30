using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;
using Files.App.Data.Commands;
using DesktopWidgets3.Views.Windows;
using Files.Core.Services;
using Files.Core.ViewModels.FolderView;
using Microsoft.UI.Xaml;
using Files.Core.Services.Settings;
using Files.App.ViewModels.UserControls;
using Microsoft.UI.Xaml.Controls;
using Files.App.Views;
using Files.App.Data.Contexts;
using System.ComponentModel;
using Files.App.ViewModels;

namespace DesktopWidgets3.ViewModels.Pages.Widget;

public partial class FolderViewViewModel : BaseWidgetViewModel<FolderViewWidgetSettings>, IWidgetUpdate, IWidgetClose, IFolderViewViewModel
{
    private bool AllowNavigation = true;

    private string FolderPath = string.Empty;

    private bool ShowIconOverlay = true;

    private readonly Files.App.App App;

    private readonly ICommandManager _commandManager;
    private readonly IModifiableCommandManager _modifiableCommandManager;
    private readonly IDialogService _dialogService;
    private readonly StatusCenterViewModel _statusCenterViewModel;
    private readonly InfoPaneViewModel _infoPaneViewModel;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IWidgetManagerService _widgetManagerService;

    private readonly IContentPageContext _contentPageContext;
    private readonly IPageContext _pageContext;

    #region interfaces

    Window IFolderViewViewModel.MainWindow => WidgetWindow;

    Page IFolderViewViewModel.Page => WidgetPage;

    IntPtr IFolderViewViewModel.WindowHandle => WidgetWindow.WindowHandle;

    Frame IFolderViewViewModel.RootFrame => (Frame)WidgetPage.Content;

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

    private int tabStripSelectedIndex = 0;
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

    public FolderViewViewModel(IWidgetManagerService widgetManagerService, ICommandManager commandManager, IModifiableCommandManager modifiableCommandManager, IDialogService dialogService, StatusCenterViewModel statusCenterViewModel, InfoPaneViewModel infoPaneViewModel, IUserSettingsService userSettingsService, IContentPageContext contentPageContext, IPageContext pageContext)
    {
        _widgetManagerService = widgetManagerService;

        NavigatedTo += FolderViewViewModel_NavigatedTo;

        // Initialize related services of Files
        App = new(this);

        _commandManager = commandManager;
        _modifiableCommandManager = modifiableCommandManager;
        _dialogService = dialogService;
        _statusCenterViewModel = statusCenterViewModel;
        _infoPaneViewModel = infoPaneViewModel;
        _userSettingsService = userSettingsService;
        _contentPageContext = contentPageContext;
        _pageContext = pageContext;

        _commandManager.Initialize(this);
        _modifiableCommandManager.Initialize(_commandManager);
        _dialogService.Initialize(this);
        _infoPaneViewModel.Initialize(this);
    }
    
    #region initialization

    public Page WidgetPage { get; set; } = null!;

    private void FolderViewViewModel_NavigatedTo(object? parameter, bool isInitialized)
    {
        if (!isInitialized)
        {
            App.OnLaunched(FolderPath);
        }
    }

    #endregion

    #region settings

    protected override void LoadSettings(FolderViewWidgetSettings settings)
    {
        if (ShowIconOverlay != settings.ShowIconOverlay)
        {
            ShowIconOverlay = settings.ShowIconOverlay;
            // TODO: Add support.
        }

        if (_userSettingsService.FoldersSettingsService.ShowHiddenItems != settings.ShowHiddenFile)
        {
            _userSettingsService.FoldersSettingsService.ShowHiddenItems = settings.ShowHiddenFile;
        }

        if (AllowNavigation != settings.AllowNavigation)
        {
            AllowNavigation = settings.AllowNavigation;
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

        // Put this last so that it will navigate to the new path even if it needs to refresh items
        if (FolderPath != settings.FolderPath)
        {
            FolderPath = settings.FolderPath;
            /*navigationFolderPaths.Clear();*/
            // TODO: Navigate to new path.
        }
    }

    public override FolderViewWidgetSettings GetSettings()
    {
        return new FolderViewWidgetSettings()
        {
            FolderPath = FolderPath,
            ShowIconOverlay = ShowIconOverlay,
            ShowHiddenFile = _userSettingsService.FoldersSettingsService.ShowHiddenItems,
            AllowNavigation = AllowNavigation,
            ShowExtension = _userSettingsService.FoldersSettingsService.ShowFileExtensions,
            DeleteConfirmationPolicy = _userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy,
            ShowThumbnail = _userSettingsService.FoldersSettingsService.ShowThumbnails,
            ConflictsResolveOption = _userSettingsService.GeneralSettingsService.ConflictsResolveOption,
        };
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

    T IFolderViewViewModel.GetService<T>() where T : class
    {
        return typeof(T) switch
        {
            Type t when t == typeof(ICommandManager) => (_commandManager as T)!,
            Type t when t == typeof(IModifiableCommandManager) => (_modifiableCommandManager as T)!,
            Type t when t == typeof(IDialogService) => (_dialogService as T)!,
            Type t when t == typeof(StatusCenterViewModel) => (_statusCenterViewModel as T)!,
            Type t when t == typeof(InfoPaneViewModel) => (_infoPaneViewModel as T)!,
            Type t when t == typeof(IUserSettingsService) => (_userSettingsService as T)!,
            Type t when t == typeof(IGeneralSettingsService) => (_userSettingsService.GeneralSettingsService as T)!,
            Type t when t == typeof(IFoldersSettingsService) => (_userSettingsService.FoldersSettingsService as T)!,
            Type t when t == typeof(IAppearanceSettingsService) => (_userSettingsService.AppearanceSettingsService as T)!,
            Type t when t == typeof(IApplicationSettingsService) => (_userSettingsService.ApplicationSettingsService as T)!,
            Type t when t == typeof(IInfoPaneSettingsService) => (_userSettingsService.InfoPaneSettingsService as T)!,
            Type t when t == typeof(ILayoutSettingsService) => (_userSettingsService.LayoutSettingsService as T)!,
            Type t when t == typeof(Files.Core.Services.Settings.IAppSettingsService) => (_userSettingsService.AppSettingsService as T)!,
            Type t when t == typeof(IContentPageContext) => (_contentPageContext as T)!,
            Type t when t == typeof(IPageContext) => (_pageContext as T)!,
            _ => null!,
        };
    }

    #endregion
}
