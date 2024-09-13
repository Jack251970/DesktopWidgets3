using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
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

    // cached widget id and index tag for widget menu
    private string _widgetId = string.Empty;
    private int _indexTag = -1;

    private const int EditModeOverlayWindowXamlWidth = 136;
    private const int EditModeOverlayWindowXamlHeight = 48;
    private OverlayWindow EditModeOverlayWindow = null!;

    private readonly List<JsonWidgetItem> _originalWidgetList = [];
    private bool _restoreMainWindow = false;

    #region widget window

    public async Task InitializeAsync()
    {
        await _widgetResourceService.InitalizeAsync();

        // enable all enabled widgets
        var widgetList = _appSettingsService.GetWidgetsList();
        foreach (var widget in widgetList)
        {
            if (widget.IsEnabled)
            {
                CreateWidgetWindow(widget);
            }
        }

        // initialize edit mode overlay window
        if (EditModeOverlayWindow == null)
        {
            EditModeOverlayWindow = WindowsExtensions.GetWindow<OverlayWindow>();
            await _activationService.ActivateWindowAsync(EditModeOverlayWindow);
            (EditModeOverlayWindow.Content as Frame)?.Navigate(typeof(EditModeOverlayPage));
        }
    }

    public async Task<int> AddWidgetAsync(string widgetId, bool refresh)
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

        // save widget item
        var widget = new JsonWidgetItem()
        {
            Id = widgetId,
            IndexTag = indexTag,
            IsEnabled = true,
            Position = new PointInt32(-1, -1),
            Size = _widgetResourceService.GetDefaultSize(widgetId),
            DisplayMonitor = DisplayMonitor.GetPrimaryMonitorInfo(),
            Settings = _widgetResourceService.GetDefaultSetting(widgetId),
        };
        await _appSettingsService.AddWidgetAsync(widget);

        // create widget window
        CreateWidgetWindow(widget);

        // update dashboard page
        if (refresh)
        {
            var parameter = new Dictionary<string, object>
            {
                { "UpdateEvent", DashboardViewModel.UpdateEvent.Add },
                { "Id", widget.Id },
                { "IndexTag", widget.IndexTag }
            };
            RefreshDashboardPage(parameter);
        }

        return indexTag;
    }

    public async Task EnableWidgetAsync(string widgetId, int indexTag)
    {
        // update widget list
        var widget = await _appSettingsService.EnableWidgetAsync(widgetId, indexTag);

        // create widget window
        CreateWidgetWindow(widget);
    }

    public async Task DisableWidgetAsync(string widgetId, int indexTag)
    {
        // update widget list
        await _appSettingsService.DisableWidgetAsync(widgetId, indexTag);

        // close widget window
        var widgetWindow = GetWidgetWindow(widgetId, indexTag);
        if (widgetWindow != null)
        {
           await CloseWidgetWindow(widgetWindow);
        }
    }

    public async Task DeleteWidgetAsync(string widgetId, int indexTag, bool refresh)
    {
        // update widget list
        await _appSettingsService.DeleteWidgetAsync(widgetId, indexTag);

        // close widget window
        var widgetWindow = GetWidgetWindow(widgetId, indexTag);
        if (widgetWindow != null)
        {
            await CloseWidgetWindow(widgetWindow);
        }

        // refresh dashboard page
        if (refresh)
        {
            var parameter = new Dictionary<string, object>
            {
                { "UpdateEvent", DashboardViewModel.UpdateEvent.Delete },
                { "Id", widgetId },
                { "IndexTag", indexTag }
            };
            RefreshDashboardPage(parameter);
        }
    }

    public async Task DisableAllWidgetsAsync()
    {
        await EnabledWidgetWindows.EnqueueOrInvokeAsync(WindowsExtensions.CloseWindow);
    }

    public bool IsWidgetEnabled(string widgetId, int indexTag)
    {
        return GetWidgetWindow(widgetId, indexTag) != null;
    }

    public BaseWidgetViewModel? GetWidgetViewModel(WidgetWindow widgetWindow)
    {
        return GetWidgetViewModel(widgetWindow.Id, widgetWindow.IndexTag);
    }

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
                viewModel.InitializeSettings(new WidgetNavigationParameter()
                {
                    Id = widgetId,
                    IndexTag = indexTag,
                    Settings = widgetSetting
                });
            }
        }
    }

    private void CreateWidgetWindow(JsonWidgetItem widget)
    {
        // configure widget window lifecycle actions
        var minSize = _widgetResourceService.GetMinSize(widget.Id);
        var maxSize = _widgetResourceService.GetMaxSize(widget.Id);
        var lifecycleActions = new WindowsExtensions.WindowLifecycleActions()
        {
            Window_Creating = () => WidgetWindow_Creating(widget),
            Window_Created = (window) => WidgetWindow_Created(window, widget, minSize, maxSize),
            Window_Closing = WidgetWindow_Closing,
            Window_Closed = () => WidgetWindow_Closed(widget)
        };

        // create widget window
        var newThread = _widgetResourceService.GetWidgetInNewThread(widget.Id);
        var widgetWindow = WindowsExtensions.GetWindow<WidgetWindow>(newThread, lifecycleActions);
    }

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
                viewModel.InitializeSettings(new WidgetNavigationParameter()
                {
                    Id = widgetId,
                    IndexTag = indexTag,
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

            // register right tapped menu
            var widgetMenu = RegisterWidgetMenu(widgetWindow);

            // show window
            widgetWindow.Show(true);

            // add to widget list & widget window list
            EnabledWidgets.Add(new WidgetWindowPair()
            {
                Window = widgetWindow,
                ViewModel = viewModel,
                Menu = widgetMenu
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
            if (viewModel is IWidgetClosing close)
            {
                close.WidgetWindow_Closing();
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

    private async Task CloseWidgetWindow(WidgetWindow widgetWindow)
    {
        // get widget id & index tag
        var widgetId = widgetWindow.Id;
        var indexTag = widgetWindow.IndexTag;

        // remove from widget list & widget window list
        EnabledWidgets.RemoveAll(x => x.Window.Id == widgetId && x.Window.IndexTag == indexTag);
        EnabledWidgetWindows.RemoveAll(x => x.Id == widgetId && x.IndexTag == indexTag);

        // close window
        await WindowsExtensions.CloseWindow(widgetWindow);
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

    private BaseWidgetViewModel? GetWidgetViewModel(string widgetId, int indexTag)
    {
        var index = EnabledWidgets.FindIndex(x => x.Window.Id == widgetId && x.Window.IndexTag == indexTag);
        if (index != -1)
        {
            return EnabledWidgets[index].ViewModel;
        }

        return null;
    }

    private MenuFlyout? GetWidgetMenu(string widgetId, int indexTag)
    {
        var index = EnabledWidgets.FindIndex(x => x.Window.Id == widgetId && x.Window.IndexTag == indexTag);
        if (index != -1)
        {
            return EnabledWidgets[index].Menu;
        }

        return null;
    }

    #endregion

    #region widget menu

    private MenuFlyout? RegisterWidgetMenu(WidgetWindow widgetWindow)
    {
        var element = widgetWindow.FrameworkElement;
        if (element is IWidgetMenu menu)
        {
            element = menu.GetWidgetMenuFrameworkElement();
        }
        if (element is not null)
        {
            WidgetProperties.SetId(element, widgetWindow.Id);
            WidgetProperties.SetIndexTag(element, widgetWindow.IndexTag);
            element.RightTapped += ShowWidgetMenu;
            return GetWidgetMenu();
        }
        return null;
    }

    private MenuFlyout GetWidgetMenu()
    {
        var menuFlyout = new MenuFlyout();
        var disableMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_DisableWidget.Text".GetLocalized()
        };
        disableMenuItem.Click += (s, e) => DisableWidget();
        menuFlyout.Items.Add(disableMenuItem);

        var deleteMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_DeleteWidget.Text".GetLocalized()
        };
        deleteMenuItem.Click += (s, e) => DeleteWidget();
        menuFlyout.Items.Add(deleteMenuItem);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        var enterMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_EnterEditMode.Text".GetLocalized()
        };
        enterMenuItem.Click += (s, e) => EnterEditMode();
        menuFlyout.Items.Add(enterMenuItem);

        return menuFlyout;
    }

    private void ShowWidgetMenu(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var widgetId = WidgetProperties.GetId(element);
            var indexTag = WidgetProperties.GetIndexTag(element);
            _widgetId = widgetId;
            _indexTag = indexTag;
            var menu = GetWidgetMenu(widgetId, indexTag);
            menu!.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private async void DisableWidget()
    {
        var widgetId = _widgetId;
        var indexTag = _indexTag;
        await DisableWidgetAsync(widgetId, indexTag);
        var parameter = new Dictionary<string, object>
        {
            { "UpdateEvent", DashboardViewModel.UpdateEvent.Disable },
            { "Id", widgetId },
            { "IndexTag", indexTag }
        };
        RefreshDashboardPage(parameter);
    }

    private async void DeleteWidget()
    {
        var widgetId = _widgetId;
        var indexTag = _indexTag;
        var widgetWindow = GetWidgetWindow(widgetId, indexTag);
        if (await widgetWindow!.ShowDeleteWidgetDialogAsync() == WidgetDialogResult.Left)
        {
            await DeleteWidgetAsync(widgetId, indexTag, true);
        }
    }

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

    public async void EnterEditMode()
    {
        // save original widget list
        _originalWidgetList.Clear();
        foreach (var widgetWindow in EnabledWidgetWindows)
        {
            _originalWidgetList.Add(new JsonWidgetItem()
            {
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
            await App.MainWindow.EnqueueOrInvokeAsync(WindowsExtensions.CloseWindow);
            _restoreMainWindow = true;
        }

        // get primary monitor info & show edit mode overlay window
        var primaryMonitorInfo = DisplayMonitor.GetPrimaryMonitorInfo();
        var screenWidth = primaryMonitorInfo.RectWork.Width;
        await EditModeOverlayWindow.EnqueueOrInvokeAsync((window) =>
        {
            // set window size according to xaml, rember larger than 136 x 39
            EditModeOverlayWindow.Size = new SizeInt32(EditModeOverlayWindowXamlWidth, EditModeOverlayWindowXamlHeight);

            // move to center top
            var windowWidth = EditModeOverlayWindow.AppWindow.Size.Width;
            EditModeOverlayWindow.Position = new PointInt32((int)((screenWidth! - windowWidth) / 2), 0);

            // show edit mode overlay window
            EditModeOverlayWindow.Show(true);
        });
    }

    public async void SaveAndExitEditMode()
    {
        // restore edit mode for all widgets
        await EnabledWidgetWindows.EnqueueOrInvokeAsync(async (window) => await window.SetEditMode(false));

        // hide edit mode overlay window
        EditModeOverlayWindow?.Hide(true);

        // restore main window if needed
        if (_restoreMainWindow)
        {
            App.MainWindow.Show();
            _restoreMainWindow = false;
        }

        // save widget list
        await Task.Run(async () =>
        {
            List<JsonWidgetItem> widgetList = [];
            foreach (var widgetWindow in EnabledWidgetWindows)
            {
                widgetList.Add(new JsonWidgetItem()
                {
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
        });
    }

    public async void CancelAndExitEditMode()
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
        EditModeOverlayWindow?.Hide(true);

        // restore main window if needed
        if (_restoreMainWindow)
        {
            App.MainWindow.Show();
            _restoreMainWindow = false;
        }
    }

    #endregion
}
