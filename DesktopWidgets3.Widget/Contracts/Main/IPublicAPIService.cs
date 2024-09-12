using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IPublicAPIService
{
    ElementTheme RootTheme { get; }

    Action<ElementTheme>? ElementTheme_Changed { get; set; }

    Task UpdateWidgetSettings(FrameworkElement element, BaseWidgetSettings settings);

    Task UpdateWidgetSettings(BaseWidgetViewModel viewModel, BaseWidgetSettings settings);
}
