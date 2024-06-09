using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Contracts.Services;

public interface IShellService
{
    IList<object>? MenuItems { get; }

    object? SettingsItem { get; }

    void Initialize(NavigationView navigationView);

    void UnregisterEvents();

    NavigationViewItem? GetSelectedItem(Type pageType);
}
