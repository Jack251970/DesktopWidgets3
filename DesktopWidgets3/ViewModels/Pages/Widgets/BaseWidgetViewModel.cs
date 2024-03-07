using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public abstract partial class BaseWidgetViewModel<T>: ObservableRecipient, INavigationAware, IWidgetSettings where T : new()
{
    protected MenuFlyout RightTappedMenu
    {
        get;
    }

    public WidgetWindow WidgetWindow
    {
        get; private set;
    } = null!;

    public WidgetType WidgetType => WidgetWindow.WidgetType;
    public int IndexTag => WidgetWindow.IndexTag;

    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    protected readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private bool _isInitialized;

    public BaseWidgetViewModel()
    {
        _navigationService = App.GetService<INavigationService>();
        _widgetManagerService = App.GetService<IWidgetManagerService>();

        RightTappedMenu = GetRightTappedMenu();
    }

    #region abstract methods

    protected abstract void LoadSettings(T settings);

    public abstract T GetSettings();

    #endregion

    #region dispatcher queue

    protected void RunOnDispatcherQueue(Action action)
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => action());
    }

    #endregion

    #region navigation aware

    public event Action<object?, bool>? NavigatedTo;
    public event Action? NavigatedFrom;

    public void OnNavigatedTo(object parameter)
    {
        var isInitialized = _isInitialized;

        // Load settings
        if (parameter is WidgetNavigationParameter navigationParameter)
        {
            WidgetWindow ??= navigationParameter.Window!;
            if (navigationParameter.Settings is T settings)
            {
                LoadSettings(settings);
                _isInitialized = true;
            }
        }

        // Make sure we have loaded settings
        if (!_isInitialized)
        {
            LoadSettings(new T());
            _isInitialized = true;
        }

        NavigatedTo?.Invoke(parameter, isInitialized);
    }

    public void OnNavigatedFrom()
    {
        NavigatedFrom?.Invoke();
    }

    #endregion

    #region widget settings

    public BaseWidgetSettings GetWidgetSettings() => (GetSettings() as BaseWidgetSettings)!;

    protected void UpdateWidgetSettings(BaseWidgetSettings settings)
    {
        _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, settings);
    }

    #endregion

    #region right tapped menu

    public void RegisterRightTappedMenu(FrameworkElement element)
    {
        element.RightTapped += ShowRightTappedMenu;
    }

    private void ShowRightTappedMenu(object sender, RightTappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            RightTappedMenu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private MenuFlyout GetRightTappedMenu()
    {
        var menuFlyout = new MenuFlyout();
        var disableMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_DisableWidget_Text".GetLocalized()
        };
        disableMenuItem.Click += (s, e) => DisableWidget();
        menuFlyout.Items.Add(disableMenuItem);

        var deleteMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_DeleteWidget_Text".GetLocalized()
        };
        deleteMenuItem.Click += (s, e) => DeleteWidget();
        menuFlyout.Items.Add(deleteMenuItem);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        var enterMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_EnterEditMode_Text".GetLocalized()
        };
        enterMenuItem.Click += (s, e) => EnterEidtMode();
        menuFlyout.Items.Add(enterMenuItem);

        return menuFlyout;
    }

    private void DisableWidget()
    {
        var widgetType = WidgetWindow.WidgetType;
        var indexTag = WidgetWindow.IndexTag;
        _widgetManagerService.DisableWidget(widgetType, indexTag);
        var dashboardPageKey = typeof(DashboardViewModel).FullName!;
        var parameter = new Dictionary<string, object>
        {
            { "UpdateEvent", DashboardViewModel.UpdateEvent.Disable },
            { "WidgetType", WidgetWindow.WidgetType },
            { "IndexTag", WidgetWindow.IndexTag }
        };
        if (_navigationService.GetCurrentPageKey() == dashboardPageKey)
        {
            _navigationService.NavigateTo(dashboardPageKey, parameter);
        }
        else
        {
            _navigationService.SetNextParameter(dashboardPageKey, parameter);
        }
    }

    private async void DeleteWidget()
    {
        if (await WidgetWindow.ShowDeleteWidgetDialog() == WidgetDialogResult.Left)
        {
            var widgetType = WidgetWindow.WidgetType;
            var indexTag = WidgetWindow.IndexTag;
            await _widgetManagerService.DeleteWidget(widgetType, indexTag);
            var dashboardPageKey = typeof(DashboardViewModel).FullName!;
            var parameter = new Dictionary<string, object>
            {
                { "UpdateEvent", DashboardViewModel.UpdateEvent.Delete },
                { "WidgetType", WidgetWindow.WidgetType },
                { "IndexTag", WidgetWindow.IndexTag }
            };
            if (_navigationService.GetCurrentPageKey() == dashboardPageKey)
            {
                _navigationService.NavigateTo(dashboardPageKey, parameter);
            }
            else
            {
                _navigationService.SetNextParameter(dashboardPageKey, parameter);
            }
        }
    }

    private void EnterEidtMode()
    {
        _widgetManagerService.EnterEditMode();
    }

    #endregion
}
