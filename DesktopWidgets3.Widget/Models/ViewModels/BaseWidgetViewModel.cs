using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Models.ViewModels;

public abstract class BaseWidgetViewModel : ObservableRecipient
{
    public string Id { get; private set; } = string.Empty;

    public int IndexTag { get; private set; } = 0;

    protected DispatcherQueue DispatcherQueue { get; private set; } = null!;

    private bool _isInitialized = false;

    #region abstract methods

    protected abstract void LoadSettings(BaseWidgetSettings settings, bool initialized);

    #endregion

    #region widget update

    public void InitializeSettings(object parameter)
    {
        // return if already initialized
        if (_isInitialized)
        {
            return;
        }

        // load settings from navigation parameter
        if (parameter is WidgetViewModelNavigationParameter navigationParameter)
        {
            Id = navigationParameter.Id;
            IndexTag = navigationParameter.IndexTag;
            DispatcherQueue = navigationParameter.DispatcherQueue;
            LoadSettings(navigationParameter.Settings, true);
            _isInitialized = true;
        }

        // force load settings if not initialized
        if (!_isInitialized)
        {
            LoadSettings(new BaseWidgetSettings(), true);
            _isInitialized = true;
        }
    }

    public void UpdateSettings(BaseWidgetSettings settings)
    {
        LoadSettings(settings, false);
    }

    #endregion
}
