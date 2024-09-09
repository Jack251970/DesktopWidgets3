using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Models.ViewModel;

public abstract partial class BaseWidgetSettingViewModel : ObservableRecipient
{
    // TODO: Check if we need WidgetWindow.
    public Window WidgetWindow { get; private set; } = null!;

    #region abstract methods

    public abstract void LoadSettings(BaseWidgetSettings settings, bool initialized);

    #endregion

    #region widget update

    public void InitializeSettings(object parameter)
    {
        if (parameter is WidgetNavigationParameter navigationParameter)
        {
            WidgetWindow = navigationParameter.Window!;
            if (navigationParameter.Settings is BaseWidgetSettings settings)
            {
                LoadSettings(settings, true);
            }
        }
    }

    public void UpdateSettings(BaseWidgetSettings settings)
    {
        LoadSettings(settings, false);
    }

    #endregion
}
