using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

public abstract partial class BaseWidgetSettingsViewModel : ObservableRecipient, INavigationAware
{
    protected WidgetType WidgetType
    {
        get;
    }

    protected int IndexTag
    {
        get;
        private set;
    }

    protected BaseWidgetSettings? WidgetSettings
    {
        get;
        private set;
    }

    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    protected bool _isInitialized
    {
        get;
        private set;
    }

    public BaseWidgetSettingsViewModel()
    {
        _navigationService = App.GetService<INavigationService>();
        _widgetManagerService = App.GetService<IWidgetManagerService>();

        WidgetType = InitializeWidgetType();
        IndexTag = -1;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is Dictionary<string, object> param)
        {
            if (param.TryGetValue("WidgetType", out var widgetTypeObj) && (WidgetType)widgetTypeObj == WidgetType && param.TryGetValue("IndexTag", out var indexTagObj))
            {
                IndexTag = (int)indexTagObj;
                WidgetSettings = await _widgetManagerService.GetWidgetSettings(WidgetType, IndexTag);
                if (WidgetSettings != null)
                {
                    InitializeWidgetSettings();
                    _isInitialized = true;
                }
            }
        }

        if (!_isInitialized)
        {
            var dashboardPageKey = typeof(DashboardViewModel).FullName!;
            _navigationService.NavigateTo(dashboardPageKey);
        }
    }

    public void OnNavigatedFrom()
    {

    }

    protected abstract WidgetType InitializeWidgetType();

    protected abstract void InitializeWidgetSettings();
}
