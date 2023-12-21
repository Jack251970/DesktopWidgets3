using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using static DesktopWidgets3.Services.DialogService;

namespace DesktopWidgets3.ViewModels.Pages.Widget;

public abstract partial class BaseWidgetViewModel<T>: ObservableRecipient, INavigationAware, IWidgetSettings where T : new()
{
    protected MenuFlyout RightTappedMenu
    {
        get;
    }

    public WidgetWindow WidgetWindow
    {
        get;
    }

    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    protected readonly DispatcherQueue _dispatcherQueue = App.DispatcherQueue;

    private bool _isInitialized;

    public BaseWidgetViewModel()
    {
        _dialogService = App.GetService<IDialogService>();
        _navigationService = App.GetService<INavigationService>();
        _widgetManagerService = App.GetService<IWidgetManagerService>();

        WidgetWindow = _widgetManagerService.GetLastWidgetWindow();

        RightTappedMenu = GetRightTappedMenu();
    }

    #region abstract methods

    protected abstract void LoadSettings(T settings);

    protected abstract T GetSettings();

    #endregion

    #region dispatcher queue

    protected void RunOnDispatcherQueue(Action action)
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => action());
    }

    #endregion

    #region interfaces

    public event EventHandler<object>? NavigatedTo;
    public event EventHandler? NavigatedFrom;

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is T settings)
        {
            LoadSettings(settings);
            NavigatedTo?.Invoke(this, parameter);
            _isInitialized = true;
            return;
        }
        if (!_isInitialized)
        {
            LoadSettings(new T());
            NavigatedTo?.Invoke(this, parameter);
            _isInitialized = true;
        }
    }

    public void OnNavigatedFrom()
    {
        NavigatedFrom?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region widget settings

    public BaseWidgetSettings GetWidgetSettings() => (GetSettings() as BaseWidgetSettings)!;

    #endregion

    #region right tapped menu

    public void ShowRightTappedMenu(object sender, RightTappedRoutedEventArgs e)
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
        if (await _dialogService.ShowDeleteWidgetDialog(WidgetWindow) == DialogResult.Left)
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
