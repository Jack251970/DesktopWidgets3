using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel(IWidgetManagerService widgetManagerService, IWidgetResourceService widgetResourceService) : ObservableRecipient, INavigationAware
{
    public ObservableCollection<DashboardWidgetItem> PinnedWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> UnpinnedWidgets { get; set; } = [];

    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private List<DashboardWidgetItem> yourWidgets = null!;

    private bool _isInitialized;

    #region Command

    [RelayCommand]
    private async Task AddWidgetAsync()
    {
        var dialog = new AddWidgetDialog()
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app.
            XamlRoot = App.MainWindow.Content.XamlRoot,
        };
        await dialog.ShowAsync();

        var newWidget = dialog.AddedWidget;

        if (newWidget != null)
        {
            if (newWidget is DesktopWidgets3WidgetDefinition newWidgetDefinition)
            {
                await _widgetManagerService.AddWidgetAsync(newWidgetDefinition.WidgetId, newWidgetDefinition.WidgetType, RefreshAddedWidget, false);
            }
            else if (newWidget is ComSafeWidgetDefinition newWidgetDefinition1)
            {
                // TODO: supprot windows widget.
            }
        }
    }

    #endregion

    #region Load

    private void LoadYourWidgets()
    {
        yourWidgets = _widgetResourceService.GetYourDashboardWidgetItems();
        foreach (var item in yourWidgets)
        {
            item.PinnedChangedCallback = OnPinnedChanged;
        }
    }

    private async void OnPinnedChanged(DashboardWidgetItem item)
    {
        if (item.Pinned)
        {
            await _widgetManagerService.PinWidgetAsync(item.Id, item.Type, item.Index);
        }
        else
        {
            await _widgetManagerService.UnpinWidgetAsync(item.Id, item.Type, item.Index, false);
        }

        var index = yourWidgets.FindIndex(x => x.Id == item.Id & x.Type == item.Type & x.Index == item.Index);
        if (index != -1)
        {
            yourWidgets[index].Pinned = item.Pinned;

            RefreshYourWidgets();
        }
    }

    #endregion

    #region Refresh

    internal void RefreshUnpinnedWidget(string widgetId, string widgetType, int widgetIndex)
    {
        var index = yourWidgets.FindIndex(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);
        if (index != -1)
        {
            yourWidgets.RemoveAt(index);

            RefreshYourWidgets();
        }
    }

    private void RefreshAddedWidget(string widgetId, string widgetType, int widgetIndex)
    {
        var widgetItem = _widgetResourceService.GetDashboardWidgetItem(widgetId, widgetType, widgetIndex);
        if (widgetItem != null)
        {
            widgetItem.PinnedChangedCallback = OnPinnedChanged;
            yourWidgets.Add(widgetItem);

            RefreshYourWidgets();
        }
    }

    private void RefreshYourWidgets()
    {
        PinnedWidgets.Clear();
        UnpinnedWidgets.Clear();
        foreach (var item in yourWidgets)
        {
            if (item.Pinned)
            {
                PinnedWidgets.Add(item);
            }
            else
            {
                UnpinnedWidgets.Add(item);
            }
        }
    }

    #endregion

    #region Navigation Aware

    public void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            LoadYourWidgets();
            RefreshYourWidgets();

            _isInitialized = true;

            return;
        }

        if (parameter is DashboardViewModelNavigationParameter navigationParameter)
        {
            var widgetId = navigationParameter.Id;
            var widgetType = navigationParameter.Type;
            var widgetIndex = navigationParameter.Index;
            switch (navigationParameter.Event)
            {
                case DashboardViewModelNavigationParameter.UpdateEvent.Pin:
                    var widgetItem = _widgetResourceService.GetDashboardWidgetItem(widgetId, widgetType, widgetIndex);
                    if (widgetItem != null)
                    {
                        widgetItem.PinnedChangedCallback = OnPinnedChanged;
                        yourWidgets.Add(widgetItem);

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Unpin:
                    var index = yourWidgets.FindIndex(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);
                    if (index != -1)
                    {
                        yourWidgets[index].Pinned = false;
                        yourWidgets[index].PinnedChangedCallback = OnPinnedChanged;

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Delete:
                    var index1 = yourWidgets.FindIndex(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);
                    if (index1 != -1)
                    {
                        yourWidgets.RemoveAt(index1);

                        RefreshYourWidgets();
                    }
                    break;
            }
        }
    }

    public void OnNavigatedFrom()
    {

    }

    #endregion
}
