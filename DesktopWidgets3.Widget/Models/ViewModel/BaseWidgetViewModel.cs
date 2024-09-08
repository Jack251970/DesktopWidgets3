using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Models.ViewModel;

public abstract class BaseWidgetViewModel<T> : ObservableRecipient, IWidgetNavigation, IWidgetSetting where T : BaseWidgetSettings, new()
{
    public Window WidgetWindow { get; private set; } = null!;

    protected bool _isInitialized;

    #region abstract methods

    protected abstract void LoadSettings(T settings);

    public abstract T GetSettings();

    #endregion

    #region widget navigation

    public event Action<object?>? NavigatedTo;
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

        NavigatedTo?.Invoke(parameter);
    }

    public void OnNavigatedFrom()
    {
        NavigatedFrom?.Invoke();
    }

    #endregion

    #region widget setting

    public BaseWidgetSettings GetWidgetSettings() => GetSettings();

    protected async void UpdateWidgetSettings(BaseWidgetSettings settings)
    {
        // TODO
        // await _widgetManagerService.UpdateWidgetSettings(WidgetType, IndexTag, settings);
    }

    #endregion
}
