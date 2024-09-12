using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Models.ViewModel;

public abstract class BaseWidgetViewModel : ObservableRecipient
{
    public string Id { get; private set; } = string.Empty;

    public int IndexTag { get; private set; } = 0;

    private bool _isInitialized = false;

    #region abstract methods

    protected abstract void LoadSettings(BaseWidgetSettings settings, bool initialized);

    #endregion

    #region widget update

    public void InitializeSettings(object parameter)
    {
        // load settings from navigation parameter
        if (parameter is WidgetNavigationParameter navigationParameter)
        {
            Id = navigationParameter.Id;
            IndexTag = navigationParameter.IndexTag;

            if (navigationParameter.Settings is BaseWidgetSettings settings)
            {
                LoadSettings(settings, true);

                _isInitialized = true;
            }
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
