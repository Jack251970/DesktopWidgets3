using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget;

public interface IWidgetService
{
    Task UpdateWidgetSettings(FrameworkElement element, BaseWidgetSettings settings, bool updateWidget, bool updateWidgetSetting);

    Task UpdateWidgetSettings(BaseWidgetViewModel viewModel, BaseWidgetSettings settings, bool updateWidget, bool updateWidgetSetting);
}
