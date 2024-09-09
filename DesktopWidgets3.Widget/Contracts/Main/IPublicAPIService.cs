using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IPublicAPIService
{
    Task UpdateWidgetSettingByWidgetFrameworkElement(FrameworkElement element, BaseWidgetSettings settings);

    Task UpdateWidgetSettingByWidgetViewModel(BaseWidgetViewModel viewModel, BaseWidgetSettings settings);

    Task UpdateWidgetSettingByWidgetSettingFrameworkElement(FrameworkElement element, BaseWidgetSettings settings);

    Task UpdateWidgetSettingByWidgetSettingViewModel(BaseWidgetSettingViewModel viewModel, BaseWidgetSettings settings);
}
