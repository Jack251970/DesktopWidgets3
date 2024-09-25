using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetManagerService(IAppSettingsService appSettingsService, INavigationService navigationService, IActivationService activationService, IWidgetResourceService widgetResourceService) : IWidgetManagerService
{
    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IActivationService _activationService = activationService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private readonly List<WidgetWindowPair> EnabledWidgets = [];
    private readonly List<WidgetWindow> EnabledWidgetWindows = [];

    private readonly List<JsonWidgetItem> _originalWidgetList = [];
    private bool _inEditMode = false;
    private bool _restoreMainWindow = false;

    #region widget window

    #region all widgets management

    public async Task RestartWidgetsAsync()
    {
        // close all widgets
        await CloseAllWidgetsAsync();

        // clear lists
        EnabledWidgets.Clear();
        EnabledWidgetWindows.Clear();

        // enable all enabled widgets
        EnableAllEnabledWidgets();
    }

    public async Task CloseAllWidgetsAsync()
    {
        await EnabledWidgetWindows.EnqueueOrInvokeAsync(WindowsExtensions.CloseWindowAsync);
    }

    public void EnableAllEnabledWidgets()
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        foreach (var widget in widgetList)
        {
            if (widget.IsEnabled)
            {
                CreateWidgetWindow(widget);
            }
        }
    }

    #endregion

    #region single widget management

    #region public

    public async Task<int> AddWidgetAsync(string widgetId, Action<string, int> action, bool updateDashboard)
    {
        var widgetList = _appSettingsService.GetWidgetsList();

        // find index tag
        var indexTags = widgetList.Where(x => x.Id == widgetId).Select(x => x.IndexTag).ToList();
        indexTags.Sort();
        var indexTag = 0;
        foreach (var tag in indexTags)
        {
            if (tag != indexTag)
            {
                break;
            }
            indexTag++;
        }

        // invoke action
        action(widgetId, indexTag);

        // create widget item
        var widget = new JsonWidgetItem()
        {
            Name = _widgetResourceService.GetWidgetName(widgetId),
            Id = widgetId,
            IndexTag = indexTag,
            IsEnabled = true,
            Position = new PointInt32(-1, -1),
            Size = _widgetResourceService.GetDefaultSize(widgetId),
            DisplayMonitor = DisplayMonitor.GetPrimaryMonitorInfo(),
            Settings = _widgetResourceService.GetDefaultSetting(widgetId),
        };

        // create widget window
        CreateWidgetWindow(widget);

        // update dashboard page
        if (updateDashboard)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Add,
                Id = widgetId,
                IndexTag = indexTag
            });
        }

        // save widget item
        await _appSettingsService.AddWidgetAsync(widget);

        return indexTag;
    }

    public async Task EnableWidgetAsync(string widgetId, int indexTag)
    {
        // get widget
        var widget = _appSettingsService.GetWidget(widgetId, indexTag);

        if (widget != null)
        {
            // create widget window
            CreateWidgetWindow(widget);

            // update widget list
            await _appSettingsService.EnableWidgetAsync(widgetId, indexTag);
        }
        else
        {
            // add widget
            await AddWidgetAsync(widgetId, (id, tag) => { }, false);
        }
    }

    public async Task DisableWidgetAsync(string widgetId, int indexTag, bool refresh)
    {
        // close widget window
        await CloseWidgetWindow(widgetId, indexTag);

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Disable,
                Id = widgetId,
                IndexTag = indexTag
            });
        }

        // update widget list
        await _appSettingsService.DisableWidgetAsync(widgetId, indexTag);
    }

    public async Task DeleteWidgetAsync(string widgetId, int indexTag, bool refresh)
    {
        // close widget window
        await CloseWidgetWindow(widgetId, indexTag);

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Delete,
                Id = widgetId,
                IndexTag = indexTag
            });
        }

        // update widget list
        await _appSettingsService.DeleteWidgetAsync(widgetId, indexTag);
    }

    #endregion

    #region private

    private void CreateWidgetWindow(JsonWidgetItem widget)
    {
        // configure widget window lifecycle actions
        (var minSize, var maxSize, var newThread) = _widgetResourceService.GetMinMaxSizeNewThread(widget.Id);
        var lifecycleActions = new WindowsExtensions.WindowLifecycleActions()
        {
            Window_Creating = () => WidgetWindow_Creating(widget),
            Window_Created = (window) => WidgetWindow_Created(window, widget, minSize, maxSize),
            Window_Closing = WidgetWindow_Closing,
            Window_Closed = () => WidgetWindow_Closed(widget)
        };

        // create widget window
        var widgetWindow = WindowsExtensions.CreateWindow<WidgetWindow>(newThread, lifecycleActions);
    }

    #region widget window lifecycle

    private async void WidgetWindow_Creating(JsonWidgetItem widgetItem)
    {
        // get widget id & index tag
        var widgetId = widgetItem.Id;

        // envoke disable widget interface
        var firstWidget = !EnabledWidgetWindows.Any(x => x.Id == widgetId);
        await _widgetResourceService.EnableWidgetAsync(widgetId, firstWidget);
    }

    private async void WidgetWindow_Created(Window window, JsonWidgetItem widgetItem, RectSize minSize, RectSize maxSize)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // activate window
            await _activationService.ActivateWindowAsync(widgetWindow);

            // get widget id & index tag
            var widgetId = widgetItem.Id;
            var indexTag = widgetItem.IndexTag;

            // set widget framework element
            var frameworkElement = _widgetResourceService.GetWidgetFrameworkElement(widgetId);
            widgetWindow.ShellPage.ViewModel.WidgetIcoPath = _widgetResourceService.GetWidgetIcoPath(widgetId);
            widgetWindow.ShellPage.ViewModel.WidgetDisplayTitle = _widgetResourceService.GetWidgetName(widgetId);
            widgetWindow.ShellPage.ViewModel.WidgetFrameworkElement = frameworkElement;

            // set widget properties
            WidgetProperties.SetId(frameworkElement, widgetId);
            WidgetProperties.SetIndexTag(frameworkElement, indexTag);

            // initialize widget window & settings
            widgetWindow.InitializeWindow(widgetItem);

            // initialize widget settings
            BaseWidgetViewModel viewModel = null!;
            if (frameworkElement is IViewModel element)
            {
                viewModel = element.ViewModel;
                viewModel.InitializeSettings(new WidgetViewModelNavigationParameter()
                {
                    Id = widgetId,
                    IndexTag = indexTag,
                    DispatcherQueue = widgetWindow.DispatcherQueue,
                    Settings = widgetItem.Settings
                });
            }

            // set window style, size and position
            widgetWindow.IsResizable = false;
            widgetWindow.MinSize = minSize;
            widgetWindow.MaxSize = maxSize;
            widgetWindow.Size = widgetItem.Size;
            if (widgetItem.Position.X != -1 && widgetItem.Position.Y != -1)
            {
                widgetWindow.Position = widgetItem.Position;
            }

            // initialize window
            widgetWindow.InitializeWindow();

            // show window
            widgetWindow.Show(true);

            // set edit mode
            await widgetWindow.SetEditMode(false);

            // add to widget list & widget window list
            EnabledWidgets.Add(new WidgetWindowPair()
            {
                Window = widgetWindow,
                ViewModel = viewModel
            });
            EnabledWidgetWindows.Add(widgetWindow);
        }
    }

    private async void WidgetWindow_Closing(Window window)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // set edit mode
            await widgetWindow.SetEditMode(false);

            // widget close event
            var viewModel = GetWidgetViewModel(widgetWindow.Id, widgetWindow.IndexTag);
            if (viewModel is IWidgetClose close)
            {
                close.OnWidgetClose();
            }
        }
    }

    private async void WidgetWindow_Closed(JsonWidgetItem widgetItem)
    {
        // get widget id & index tag
        var widgetId = widgetItem.Id;

        // envoke disable widget interface
        var lastWidget = !EnabledWidgetWindows.Any(x => x.Id == widgetId);
        await _widgetResourceService.DisableWidgetAsync(widgetId, lastWidget);
    }

    #endregion

    private async Task CloseWidgetWindow(string widgetId, int indexTag)
    {
        // close widget window
        var widgetWindow = GetWidgetWindow(widgetId, indexTag);
        if (widgetWindow != null)
        {
            // close window
            await WindowsExtensions.CloseWindowAsync(widgetWindow);

            // remove from widget list & widget window list
            EnabledWidgets.RemoveAll(x => x.Window.Id == widgetId && x.Window.IndexTag == indexTag);
            EnabledWidgetWindows.RemoveAll(x => x.Id == widgetId && x.IndexTag == indexTag);
        }
    }

    private WidgetWindow? GetWidgetWindow(string widgetId, int indexTag)
    {
        var index = EnabledWidgetWindows.FindIndex(x => x.Id == widgetId && x.IndexTag == indexTag);
        if (index != -1)
        {
            return EnabledWidgetWindows[index];
        }

        return null;
    }

    #endregion

    #endregion

    #endregion

    #region widget view model

    public BaseWidgetViewModel? GetWidgetViewModel(WidgetWindow widgetWindow)
    {
        return GetWidgetViewModel(widgetWindow.Id, widgetWindow.IndexTag);
    }

    private BaseWidgetViewModel? GetWidgetViewModel(string widgetId, int indexTag)
    {
        var index = EnabledWidgets.FindIndex(x => x.Window.Id == widgetId && x.Window.IndexTag == indexTag);
        if (index != -1)
        {
            return EnabledWidgets[index].ViewModel;
        }

        return null;
    }

    #endregion

    #region widget setting page

    public void NavigateToWidgetSettingPage(string widgetId, int indexTag)
    {
        // navigate to widget setting page
        _navigationService.NavigateTo(typeof(WidgetSettingViewModel).FullName!);

        // set widget setting framework element
        var frameworkElement = _widgetResourceService.GetWidgetSettingFrameworkElement(widgetId);
        var widgetSettingPage = _navigationService.Frame?.Content as WidgetSettingPage;
        if (widgetSettingPage != null)
        {
            widgetSettingPage.ViewModel.WidgetFrameworkElement = frameworkElement;
            var widgetName = _widgetResourceService.GetWidgetName(widgetId);
            NavigationViewHeaderBehavior.SetHeaderLocalize(widgetSettingPage, false);
            NavigationViewHeaderBehavior.SetHeaderContext(widgetSettingPage, widgetName);
        }

        // set widget properties
        WidgetProperties.SetId(frameworkElement, widgetId);
        WidgetProperties.SetIndexTag(frameworkElement, indexTag);

        // initialize widget settings
        if (frameworkElement is ISettingViewModel element)
        {
            var viewModel = element.ViewModel;
            var widgetSetting = GetWidgetSettings(widgetId, indexTag);
            if (widgetSetting != null)
            {
                viewModel.InitializeSettings(new WidgetViewModelNavigationParameter()
                {
                    Id = widgetId,
                    IndexTag = indexTag,
                    DispatcherQueue = App.MainWindow.DispatcherQueue,
                    Settings = widgetSetting
                });
            }
        }
    }

    #endregion

    #region widget menu

    public void AddWidgetItemsToWidgetMenu(MenuFlyout menuFlyout, WidgetWindow widgetWindow)
    {
        AddDisableDeleteItemsToWidgetMenu(menuFlyout, widgetWindow);
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        AddLayoutItemsToWidgetMenu(menuFlyout, widgetWindow);
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        AddRestartItemsToWidgetMenu(menuFlyout, widgetWindow);
    }

    #region Disable & Delete

    private void AddDisableDeleteItemsToWidgetMenu(MenuFlyout menuFlyout, WidgetWindow widgetWindow)
    {
        var disableIcon = new FontIcon()
        {
            Glyph = "\uE71A"
        };
        var disableWidgetMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_DisableWidget.Text".GetLocalized(),
            Icon = disableIcon
        };
        disableWidgetMenuItem.Click += DisableWidget;
        menuFlyout.Items.Add(disableWidgetMenuItem);

        var deleteIcon = new FontIcon()
        {
            Glyph = "\uE74D"
        };
        var deleteWidgetMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_DeleteWidget.Text".GetLocalized(),
            Icon = deleteIcon
        };
        deleteWidgetMenuItem.Click += DeleteWidget;
        menuFlyout.Items.Add(deleteWidgetMenuItem);
    }

    private async void DisableWidget(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            var widgetId = widgetWindow.Id;
            var indexTag = widgetWindow.IndexTag;
            await DisableWidgetAsync(widgetId, indexTag, true);
        }
    }

    private async void DeleteWidget(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            var widgetId = widgetWindow.Id;
            var indexTag = widgetWindow.IndexTag;
            if (await DialogFactory.ShowDeleteWidgetDialogAsync() == WidgetDialogResult.Left)
            {
                await DeleteWidgetAsync(widgetId, indexTag, true);
            }
        }
    }

    #endregion

    #region Layout

    private void AddLayoutItemsToWidgetMenu(MenuFlyout menuFlyout, WidgetWindow widgetWindow)
    {
        var layoutIcon = new FontIcon()
        {
            Glyph = "\uF0E2"
        };
        var editLayoutMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Icon = layoutIcon,
            Text = "MenuFlyoutItem_EditWidgetsLayout.Text".GetLocalized()
        };
        editLayoutMenuItem.Click += EditWidgetsLayout;
        menuFlyout.Items.Add(editLayoutMenuItem);
    }

    private void EditWidgetsLayout(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow)
        {
            EnterEditMode();
        }  
    }

    #endregion

    #region Restart

    private void AddRestartItemsToWidgetMenu(MenuFlyout menuFlyout, WidgetWindow widgetWindow)
    {
        var restartIcon = new FontIcon()
        {
            Glyph = "\uE72C"
        };
        var restartWidgetMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_RestartWidget.Text".GetLocalized(),
            Icon = restartIcon,
        };
        restartWidgetMenuItem.Click += RestartWidget;
        menuFlyout.Items.Add(restartWidgetMenuItem);

        restartIcon = new FontIcon()
        {
            Glyph = "\uE72C"
        };
        var restartWidgetsMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_RestartWidgets.Text".GetLocalized(),
            Icon = restartIcon
        };
        restartWidgetsMenuItem.Click += RestartWidgets;
        menuFlyout.Items.Add(restartWidgetsMenuItem);
    }

    private async void RestartWidget(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            var widgetId = widgetWindow.Id;
            var indexTag = widgetWindow.IndexTag;
            await CloseWidgetWindow(widgetId, indexTag);
            var widgetList = _appSettingsService.GetWidgetsList();
            var widget = widgetList.FirstOrDefault(x => x.Id == widgetId && x.IndexTag == indexTag);
            if (widget != null)
            {
                CreateWidgetWindow(widget);
            }
        }
    }

    private async void RestartWidgets(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow)
        {
            await RestartWidgetsAsync();
        }
    }

    #endregion

    #endregion

    #region widget settings

    public BaseWidgetSettings? GetWidgetSettings(string widgetId, int indexTag)
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => x.Id == widgetId && x.IndexTag == indexTag);

        return widget?.Settings.Clone();
    }

    public async Task UpdateWidgetSettingsAsync(string widgetId, int indexTag, BaseWidgetSettings settings, bool updateWidget, bool updateWidgetSetting)
    {
        // update widget window
        if (updateWidget)
        {
            var widgetWindow = GetWidgetWindow(widgetId, indexTag);
            if (widgetWindow != null)
            {
                var viewModel = GetWidgetViewModel(widgetId, indexTag);
                await widgetWindow.EnqueueOrInvokeAsync((window) =>
                {
                    viewModel?.UpdateSettings(settings);
                });
            }
        }

        // update widget setting
        if (updateWidgetSetting)
        {
            var widgetSettingPage = _navigationService.Frame?.Content as WidgetSettingPage;
            if (widgetSettingPage != null)
            {
                var settingFrameworkElement = widgetSettingPage.ViewModel.WidgetFrameworkElement;
                if (settingFrameworkElement is ISettingViewModel settingViewModel)
                {
                    var viewModel = settingViewModel.ViewModel;
                    if (viewModel.Id == widgetId && viewModel.IndexTag == indexTag)
                    {
                        viewModel.UpdateSettings(settings);
                    }
                }
            }
        }

        // update widget list
        await _appSettingsService.UpdateWidgetSettingsAsync(widgetId, indexTag, settings);
    }

    #endregion

    #region edit mode

    public bool InEditMode()
    {
        return _inEditMode;
    }

    public async void EnterEditMode()
    {
        // save original widget list
        _originalWidgetList.Clear();
        foreach (var widgetWindow in EnabledWidgetWindows)
        {
            _originalWidgetList.Add(new JsonWidgetItem()
            {
                Name = _widgetResourceService.GetWidgetName(widgetWindow.Id),
                Id = widgetWindow.Id,
                IndexTag = widgetWindow.IndexTag,
                IsEnabled = true,
                Position = widgetWindow.Position,
                Size = widgetWindow.Size,
                DisplayMonitor = DisplayMonitor.GetMonitorInfo(widgetWindow),
                Settings = null!,
            });
        }

        // set edit mode for all widgets
        await EnabledWidgetWindows.EnqueueOrInvokeAsync(async (window) => await window.SetEditMode(true));

        // hide main window if visible
        if (App.MainWindow.Visible)
        {
            await App.MainWindow.EnqueueOrInvokeAsync(WindowsExtensions.CloseWindowAsync);
            _restoreMainWindow = true;
        }

        // get primary monitor info & show edit mode overlay window
        await App.EditModeWindow.EnqueueOrInvokeAsync((window) =>
        {
            // move to center top
            window.CenterTopOnMonitor();

            // show edit mode overlay window
            window.Show(true);
        });

        _inEditMode = true;
    }

    public async Task SaveAndExitEditMode()
    {
        // restore edit mode for all widgets
        await EnabledWidgetWindows.EnqueueOrInvokeAsync(async (window) => await window.SetEditMode(false));

        // hide edit mode overlay window
        App.EditModeWindow?.Hide(true);

        // restore main window if needed
        if (_restoreMainWindow)
        {
            App.MainWindow.Show();
            _restoreMainWindow = false;
        }

        // save widget list
        List<JsonWidgetItem> widgetList = [];
        foreach (var widgetWindow in EnabledWidgetWindows)
        {
            widgetList.Add(new JsonWidgetItem()
            {
                Name = _widgetResourceService.GetWidgetName(widgetWindow.Id),
                Id = widgetWindow.Id,
                IndexTag = widgetWindow.IndexTag,
                IsEnabled = true,
                Position = widgetWindow.Position,
                Size = widgetWindow.Size,
                DisplayMonitor = DisplayMonitor.GetMonitorInfo(widgetWindow),
                Settings = null!,
            });
        }
        await _appSettingsService.UpdateWidgetsListIgnoreSettingsAsync(widgetList);

        _inEditMode = false;
    }

    public async void CancelChangesAndExitEditMode()
    {
        // restore position, size, edit mode for all widgets
        await EnabledWidgetWindows.EnqueueOrInvokeAsync(async (window) =>
        {
            // set edit mode for all widgets
            await window.SetEditMode(false);

            // read original position and size
            var originalWidget = _originalWidgetList.First(x => x.Id == window.Id && x.IndexTag == window.IndexTag);

            // restore position and size
            if (originalWidget != null)
            {
                window.Position = originalWidget.Position;
                window.Size = originalWidget.Size;
                window.Show(true);
            };
        });

        // hide edit mode overlay window
        App.EditModeWindow?.Hide(true);

        // restore main window if needed
        if (_restoreMainWindow)
        {
            App.MainWindow.Show();
            _restoreMainWindow = false;
        }

        _inEditMode = false;
    }

    #endregion

    #region widget message

    private void RefreshDashboardPage(object parameter)
    {
        var dashboardPageKey = typeof(DashboardViewModel).FullName!;
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            var currentKey = _navigationService.GetCurrentPageKey();
            if (currentKey == dashboardPageKey)
            {
                _navigationService.NavigateTo(dashboardPageKey, parameter);
            }
            else
            {
                _navigationService.SetNextParameter(dashboardPageKey, parameter);
            }
        });
    }

    #endregion
}
