using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IPublicAPIService
{
    Task UpdateWidgetSettings(FrameworkElement element, BaseWidgetSettings settings);

    Task UpdateWidgetSettings(BaseWidgetViewModel viewModel, BaseWidgetSettings settings);
}
