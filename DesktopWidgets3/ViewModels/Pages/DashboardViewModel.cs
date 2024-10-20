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
                // TODO
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

    internal void RefreshAddedWidget(string widgetId, string widgetType, int indexTag)
    {
        var widgetItem = _widgetResourceService.GetDashboardWidgetItem(widgetId, widgetType, indexTag);
        if (widgetItem != null)
        {
            widgetItem.PinnedChangedCallback = OnPinnedChanged;
            yourWidgets.Add(widgetItem);

            RefreshYourWidgets();
        }
    }

    internal void RefreshUnpinnedWidget(string widgetId, string widgetType, int indexTag)
    {
        var index = yourWidgets.FindIndex(x => x.Id == widgetId & x.Type == widgetType & x.Index == indexTag);
        if (index != -1)
        {
            yourWidgets.RemoveAt(index);

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
            var indexTag = navigationParameter.Index;
            switch (navigationParameter.Event)
            {
                case DashboardViewModelNavigationParameter.UpdateEvent.Add:
                    var widgetItem = _widgetResourceService.GetDashboardWidgetItem(widgetId, widgetType, indexTag);
                    if (widgetItem != null)
                    {
                        widgetItem.PinnedChangedCallback = OnPinnedChanged;
                        yourWidgets.Add(widgetItem);

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Unpin:
                    var widgetIndex = yourWidgets.FindIndex(x => x.Id == widgetId & x.Type == widgetType & x.Index == indexTag);
                    if (widgetIndex != -1)
                    {
                        yourWidgets[widgetIndex].Pinned = false;
                        yourWidgets[widgetIndex].PinnedChangedCallback = OnPinnedChanged;

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Delete:
                    widgetIndex = yourWidgets.FindIndex(x => x.Id == widgetId & x.Type == widgetType & x.Index == indexTag);
                    if (widgetIndex != -1)
                    {
                        yourWidgets.RemoveAt(widgetIndex);

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
