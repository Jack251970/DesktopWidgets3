﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Graphics;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetManagerService(IActivationService activationService, IAppSettingsService appSettingsService, INavigationService navigationService, IWidgetResourceService widgetResourceService) : IWidgetManagerService
{
    private readonly IActivationService _activationService = activationService;
    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private readonly Dictionary<string, (string, string, int)> PinnedWidgetRuntimeIds = [];

    private readonly List<WidgetWindowPair> PinnedWidgetWindowPairs = [];
    private readonly List<WidgetWindow> PinnedWidgetWindows = [];

    private readonly List<JsonWidgetItem> _originalWidgetList = [];
    private bool _inEditMode = false;
    private bool _restoreMainWindow = false;

    #region Widget Info

    #region Runtime Id

    public (string widgetId, string widgetType, int widgetIndex) GetWidgetInfo(string widgetRuntimeId)
    {
        if (PinnedWidgetRuntimeIds.TryGetValue(widgetRuntimeId, out var value))
        {
            return value;
        }

        return (string.Empty, string.Empty, -1);
    }

    private string GetWidgetRuntimeId(string widgetId, string widgetType, int widgetIndex)
    {
        foreach (var (widgetRuntimeId, widgetInfo) in PinnedWidgetRuntimeIds)
        {
            if (widgetInfo == (widgetId, widgetType, widgetIndex))
            {
                return widgetRuntimeId;
            }
        }

        return string.Empty;
    }

    #endregion

    #region Widget Info & Context

    public WidgetInfo? GetWidgetInfo(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(widgetId, widgetType, widgetIndex);

        // get widget info
        if (widgetRuntimeId != string.Empty)
        {
            var widgetInfo = PinnedWidgetWindowPairs.FirstOrDefault(x => x.Window.RuntimeId == widgetRuntimeId)?.WidgetInfo;
            if (widgetInfo != null)
            {
                return widgetInfo;
            }
        }

        return null;
    }

    public WidgetContext? GetWidgetContext(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget info
        var widgetInfo = GetWidgetInfo(widgetId, widgetType, widgetIndex);

        // get widget context
        if (widgetInfo != null)
        {
            return (WidgetContext)widgetInfo.WidgetContext;
        }

        return null;
    }

    #endregion

    #region Is Active

    public bool GetWidgetIsActive(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(widgetId, widgetType, widgetIndex);

        // get widget window active state
        var index = PinnedWidgetWindowPairs.FindIndex(x => x.Window.RuntimeId == widgetRuntimeId);
        if (index != -1)
        {
            return PinnedWidgetWindowPairs[index].Window.IsActive;
        }

        return false;
    }

    #endregion

    #endregion

    #region Widget Window

    #region All Widgets Management

    public void InitializePinnedWidgets()
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        foreach (var widget in widgetList)
        {
            if (widget.Pinned)
            {
                CreateWidgetWindow(widget);
            }
        }
    }

    public async Task RestartWidgetsAsync()
    {
        // close all widgets
        await CloseAllWidgetsAsync();

        // clear lists
        PinnedWidgetRuntimeIds.Clear();
        PinnedWidgetWindowPairs.Clear();
        PinnedWidgetWindows.Clear();

        // enable all enabled widgets
        InitializePinnedWidgets();
    }

    public async Task CloseAllWidgetsAsync()
    {
        await PinnedWidgetWindows.EnqueueOrInvokeAsync(WindowsExtensions.CloseWindowAsync, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);
    }

    #endregion

    #region Single Widget Management

    #region Public

    public async Task AddWidgetAsync(string widgetId, string widgetType, Action<string, string, int> action, bool updateDashboard)
    {
        var widgetList = _appSettingsService.GetWidgetsList();

        // find index tag
        var indexs = widgetList.Where(x => x.Id == widgetId & x.Type == widgetType).Select(x => x.Index).ToList();
        indexs.Sort();
        var index = 0;
        foreach (var tag in indexs)
        {
            if (tag != index)
            {
                break;
            }
            index++;
        }

        // invoke action
        action(widgetId, widgetType, index);

        // create widget item
        var widget = new JsonWidgetItem()
        {
            Name = _widgetResourceService.GetWidgetName(widgetId, widgetType),
            Id = widgetId,
            Type = widgetType,
            Index = index,
            Pinned = true,
            Position = new PointInt32(-10000, -10000),
            Size = _widgetResourceService.GetWidgetDefaultSize(widgetId, widgetType),
            DisplayMonitor = DisplayMonitor.GetPrimaryMonitorInfo(),
            Settings = _widgetResourceService.GetDefaultSettings(widgetId, widgetType),
        };

        // create widget window
        var widgetRuntimeId = CreateWidgetWindow(widget);

        // update dashboard page
        if (updateDashboard)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Pin,
                Id = widgetId,
                Type = widgetType,
                Index = index
            });
        }

        // save widget item
        await _appSettingsService.AddWidgetAsync(widget);
    }

    public async Task PinWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        // get widget
        var widget = _appSettingsService.GetWidget(widgetId, widgetType, widgetIndex);

        // pin widget
        if (widget != null)
        {
            // create widget window
            CreateWidgetWindow(widget);

            // update widget list
            await _appSettingsService.PinWidgetAsync(widgetId, widgetType, widgetIndex);
        }
        else
        {
            // add widget
            await AddWidgetAsync(widgetId, widgetType, (id, type, tag) => { }, false);
        }

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Pin,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex
            });
        }
    }

    public async Task UnpinWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(widgetId, widgetType, widgetIndex);

        // close widget window
        await CloseWidgetWindow(widgetRuntimeId);

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Unpin,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex
            });
        }

        // update widget list
        await _appSettingsService.UnpinWidgetAsync(widgetId, widgetType, widgetIndex);
    }

    public async Task DeleteWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(widgetId, widgetType, widgetIndex);

        // close widget window
        await CloseWidgetWindow(widgetRuntimeId);

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Delete,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex
            });
        }

        // update widget list
        await _appSettingsService.DeleteWidgetAsync(widgetId, widgetType, widgetIndex);
    }

    #endregion

    #region Private

    private string CreateWidgetWindow(JsonWidgetItem item)
    {
        // create widget info & register guid
        var id = StringUtils.GetRandomWidgetRuntimeId();
        var widgetContext = new WidgetContext(this)
        {
            Id = id,
            Type = item.Type,
        };
        var widgetInfo = new WidgetInfo(this)
        {
            WidgetContext = widgetContext
        };

        // configure widget window lifecycle actions
        (var minSize, var maxSize) = _widgetResourceService.GetWidgetMinMaxSize(item.Id, item.Type);
        var lifecycleActions = new WindowsExtensions.WindowLifecycleActions()
        {
            Window_Creating = null,
            Window_Created = (window) => WidgetWindow_Created(widgetInfo, window, item, minSize, maxSize),
            Window_Closing = WidgetWindow_Closing,
            Window_Closed = null
        };

        // create widget window
        var widgetWindow = WindowsExtensions.CreateWindow<WidgetWindow>(_appSettingsService.MultiThread, lifecycleActions, item);

        return id;
    }

    #region Widget Window Lifecycle

    private async void WidgetWindow_Created(WidgetInfo widgetInfo, Window window, JsonWidgetItem item, RectSize minSize, RectSize maxSize)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // activate window
            await _activationService.ActivateWindowAsync(widgetWindow);

            // get widget id & index tag
            var widgetId = item.Id;
            var widgetType = item.Type;
            var widgetIndex = item.Index;

            // set widget ico & title & framework element
            widgetWindow.ViewModel.WidgetIconPath = _widgetResourceService.GetWidgetIconPath(widgetId, widgetType);
            widgetWindow.ViewModel.WidgetDisplayTitle = _widgetResourceService.GetWidgetName(widgetId, widgetType);

            // initialize window
            var menuFlyout = GetWidgetMenuFlyout(widgetWindow);
            widgetWindow.Initialize(widgetInfo.WidgetContext.Id, item, menuFlyout);

            // set window style, size and position
            widgetWindow.IsResizable = false;
            widgetWindow.MinSize = minSize;
            widgetWindow.MaxSize = maxSize;
            widgetWindow.Size = item.Size;
            WindowExtensions.Move(widgetWindow, -10000, -10000);

            // register load event handler
            widgetWindow.LoadCompleted += WidgetWindow_LoadCompleted;

            // activate window
            widgetWindow.Activate();

            // add to widget list & widget window list
            PinnedWidgetRuntimeIds.Add(widgetInfo.WidgetContext.Id, (widgetId, widgetType, widgetIndex));
            PinnedWidgetWindowPairs.Add(new WidgetWindowPair()
            {
                WidgetInfo = widgetInfo,
                Window = widgetWindow,
                ViewModel = null,
                MenuFlyout = menuFlyout
            });
            PinnedWidgetWindows.Add(widgetWindow);
        }
    }

    private void WidgetWindow_LoadCompleted(object? sender, WidgetWindow.LoadCompletedEventArgs args)
    {
        if (sender is WidgetWindow widgetWindow)
        {
            // unregister load event handler
            widgetWindow.LoadCompleted -= WidgetWindow_LoadCompleted;

            // parse event agrs
            var widgetRuntimeId = args.WidgetRuntimeId;
            var widgetPosition = args.WidgetPosition;
            var widgetSettings = args.WidgetSettings;

            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetRuntimeId);

            // set widget position
            widgetWindow.Position = widgetPosition;

            // get widget framework element
            var widgetContext = GetWidgetContext(widgetId, widgetType, widgetIndex);
            var frameworkElement = _widgetResourceService.GetWidgetContent(widgetId, widgetType, widgetContext!);

            // initialize widget settings
            BaseWidgetViewModel viewModel = null!;
            if (frameworkElement is IViewModel element)
            {
                viewModel = element.ViewModel;
                viewModel.InitializeSettings(new WidgetViewModelNavigationParameter()
                {
                    Id = widgetId,
                    Type = widgetType,
                    Index = widgetIndex,
                    DispatcherQueue = widgetWindow.DispatcherQueue,
                    Settings = widgetSettings
                });
            }

            // set widget framework element
            widgetWindow.ViewModel.WidgetFrameworkElement = frameworkElement;

            // add to widget list
            var index = PinnedWidgetWindowPairs.FindIndex(x => x.Window.RuntimeId == widgetWindow.RuntimeId);
            if (index != -1)
            {
                PinnedWidgetWindowPairs[index].ViewModel = viewModel;
            }
        }
    }

    private void WidgetWindow_Closing(Window window)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // widget close event
            var viewModel = GetWidgetViewModel(widgetWindow.RuntimeId);
            if (viewModel is IWidgetWindowClose close)
            {
                close.WidgetWindowClosing();
            }
        }
    }

    #endregion

    private async Task CloseWidgetWindow(string widgetRuntimeId)
    {
        // close widget window
        var widgetWindow = GetWidgetWindow(widgetRuntimeId);
        if (widgetWindow != null)
        {
            // close window
            await WindowsExtensions.CloseWindowAsync(widgetWindow);

            // remove from widget list & widget window list
            PinnedWidgetRuntimeIds.Remove(widgetRuntimeId);
            PinnedWidgetWindowPairs.RemoveAll(x => x.Window.RuntimeId == widgetRuntimeId);
            PinnedWidgetWindows.RemoveAll(x => x.RuntimeId == widgetRuntimeId);
        }
    }

    private WidgetWindow? GetWidgetWindow(string widgetRuntimeId)
    {
        if (string.IsNullOrEmpty(widgetRuntimeId))
        {
            return null;
        }

        var index = PinnedWidgetWindows.FindIndex(x => x.RuntimeId == widgetRuntimeId);
        if (index != -1)
        {
            return PinnedWidgetWindows[index];
        }

        return null;
    }

    #endregion

    #endregion

    #endregion

    #region Widget View Model

    public BaseWidgetViewModel? GetWidgetViewModel(WidgetWindow widgetWindow)
    {
        return GetWidgetViewModel(widgetWindow.RuntimeId);
    }

    private BaseWidgetViewModel? GetWidgetViewModel(string widgetRuntimeId)
    {
        if (string.IsNullOrEmpty(widgetRuntimeId))
        {
            return null;
        }

        var index = PinnedWidgetWindowPairs.FindIndex(x => x.Window.RuntimeId == widgetRuntimeId);
        if (index != -1)
        {
            return PinnedWidgetWindowPairs[index].ViewModel;
        }

        return null;
    }

    #endregion

    #region Widget Setting Page

    public void NavigateToWidgetSettingPage(string widgetId, string widgetType, int widgetIndex)
    {
        // navigate to widget setting page
        _navigationService.NavigateTo(typeof(WidgetSettingViewModel).FullName!);

        // set widget setting framework element
        var frameworkElement = _widgetResourceService.GetWidgetSettingContent(widgetId, widgetType);
        var widgetSettingPage = _navigationService.Frame?.Content as WidgetSettingPage;
        if (widgetSettingPage != null)
        {
            widgetSettingPage.ViewModel.WidgetFrameworkElement = frameworkElement;
            var widgetName = _widgetResourceService.GetWidgetName(widgetId, widgetType);
            NavigationViewHeaderBehavior.SetHeaderLocalize(widgetSettingPage, false);
            NavigationViewHeaderBehavior.SetHeaderContext(widgetSettingPage, widgetName);
        }

        // initialize widget settings
        if (frameworkElement is ISettingViewModel element)
        {
            var viewModel = element.ViewModel;
            var widgetSetting = GetWidgetSettings(widgetId, widgetType, widgetIndex);
            if (widgetSetting != null)
            {
                viewModel.InitializeSettings(new WidgetViewModelNavigationParameter()
                {
                    Id = widgetId,
                    Type = widgetType,
                    Index = widgetIndex,
                    DispatcherQueue = App.MainWindow.DispatcherQueue,
                    Settings = widgetSetting
                });
            }
        }
    }

    #endregion

    #region Widget Menu

    private MenuFlyout GetWidgetMenuFlyout(WidgetWindow widgetWindow)
    {
        var menuFlyout = new MenuFlyout
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
        };

        AddUnpinDeleteItemsToWidgetMenu(menuFlyout, widgetWindow);
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        AddLayoutItemsToWidgetMenu(menuFlyout, widgetWindow);
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        AddRestartItemsToWidgetMenu(menuFlyout, widgetWindow);

        return menuFlyout;
    }

    #region Unpin & Delete

    private void AddUnpinDeleteItemsToWidgetMenu(MenuFlyout menuFlyout, WidgetWindow widgetWindow)
    {
        var unpinIcon = new FontIcon()
        {
            Glyph = "\uE77A"
        };
        var unpinWidgetMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_UnpinWidget.Text".GetLocalized(),
            Icon = unpinIcon
        };
        unpinWidgetMenuItem.Click += UnpinWidget;
        menuFlyout.Items.Add(unpinWidgetMenuItem);

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

    private async void UnpinWidget(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            // get widget runtime id
            var widgetRuntimeId = widgetWindow.RuntimeId;

            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetRuntimeId);

            // unpin widget
            await UnpinWidgetAsync(widgetId, widgetType, widgetIndex, true);
        }
    }

    private async void DeleteWidget(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            // get widget runtime id
            var widgetRuntimeId = widgetWindow.RuntimeId;

            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetRuntimeId);

            // delete widget
            await DialogFactory.ShowDeleteWidgetFullScreenDialogAsync(async () => await DeleteWidgetAsync(widgetId, widgetType, widgetIndex, true));
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
            var widgetRuntimeId = widgetWindow.RuntimeId;

            await CloseWidgetWindow(widgetRuntimeId);

            var widgetList = _appSettingsService.GetWidgetsList();

            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetRuntimeId);
            var widget = widgetList.FirstOrDefault(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);
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

    #region Widget Settings

    public BaseWidgetSettings? GetWidgetSettings(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget settings
        var widgetList = _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);

        return widget?.Settings.Clone();
    }

    public async Task UpdateWidgetSettingsAsync(string widgetId, string widgetType, int widgetIndex, BaseWidgetSettings settings, bool updateWidget, bool updateWidgetSetting)
    {
        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(widgetId, widgetType, widgetIndex);

        // update widget window
        if (updateWidget)
        {
            var widgetWindow = GetWidgetWindow(widgetRuntimeId);
            if (widgetWindow != null)
            {
                var viewModel = GetWidgetViewModel(widgetRuntimeId);
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
                    if (viewModel.Id == widgetId & viewModel.Index == widgetIndex)
                    {
                        viewModel.UpdateSettings(settings);
                    }
                }
            }
        }

        // update widget list
        await _appSettingsService.UpdateWidgetSettingsAsync(widgetId, widgetType, widgetIndex, settings);
    }

    #endregion

    #region Widget Edit Mode

    public async void EnterEditMode()
    {
        // save original widget list
        _originalWidgetList.Clear();
        foreach (var widgetWindow in PinnedWidgetWindows)
        {
            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetWindow.RuntimeId);
            _originalWidgetList.Add(new JsonWidgetItem()
            {
                Name = _widgetResourceService.GetWidgetName(widgetId, widgetType),
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Pinned = true,
                Position = widgetWindow.Position,
                Size = widgetWindow.Size,
                DisplayMonitor = DisplayMonitor.GetMonitorInfo(widgetWindow),
                Settings = null!,
            });
        }

        // set flag
        _inEditMode = true;

        // set edit mode for all widgets
        await PinnedWidgetWindows.EnqueueOrInvokeAsync(async (window) =>
        {
            await window.SetEditMode(true);
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        // hide main window & show edit mode overlay window
        await App.MainWindow.EnqueueOrInvokeAsync(async (window) =>
        {
            // hide main window
            if (window.Visible)
            {
                await WindowsExtensions.CloseWindowAsync(window);
                _restoreMainWindow = true;
            }

            // show edit mode overlay window
            App.EditModeWindow.Show();
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);
    }

    public async Task SaveAndExitEditMode()
    {
        // restore edit mode for all widgets
        await PinnedWidgetWindows.EnqueueOrInvokeAsync(async (window) =>
        {
            await window.SetEditMode(false);
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        // hide edit mode overlay window
        App.EditModeWindow?.Hide();

        // restore main window if needed
        if (_restoreMainWindow)
        {
            App.MainWindow.Show();
            _restoreMainWindow = false;
        }

        // save widget list
        List<JsonWidgetItem> widgetList = [];
        foreach (var widgetWindow in PinnedWidgetWindows)
        {
            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetWindow.RuntimeId);
            widgetList.Add(new JsonWidgetItem()
            {
                Name = _widgetResourceService.GetWidgetName(widgetId, widgetType),
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Pinned = true,
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
        await PinnedWidgetWindows.EnqueueOrInvokeAsync(async (window) =>
        {
            // set edit mode for all widgets
            await window.SetEditMode(false);

            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(window.RuntimeId);

            // read original position and size
            var originalWidget = _originalWidgetList.FirstOrDefault(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);

            // restore position and size
            if (originalWidget != null)
            {
                window.Position = originalWidget.Position;
                window.Size = originalWidget.Size;
                window.Show();
            };
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        // hide edit mode overlay window
        App.EditModeWindow?.Hide();

        // restore main window if needed
        if (_restoreMainWindow)
        {
            App.MainWindow.Show();
            _restoreMainWindow = false;
        }

        _inEditMode = false;
    }

    public async Task CheckEditModeAsync()
    {
        if (_inEditMode)
        {
            App.ShowMainWindow(false);
            if (await DialogFactory.ShowQuitEditModeDialogAsync(App.MainWindow) == WidgetDialogResult.Left)
            {
                await SaveAndExitEditMode();
            }
        }
    }

    #endregion

    #region Dashboard

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
