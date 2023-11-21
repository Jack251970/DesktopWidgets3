using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DesktopWidgets3.Contracts.Services;

public interface ISubNavigationService
{
    event NavigatedEventHandler Navigated;

    Frame? GetFrame(Type parentPage);

    void SetFrame(Type parentPage, Frame frame);

    bool NavigateTo(Type pageType, object? parameter = null, bool clearNavigation = true);

    void InitializeDefaultPage(Type pageType, object? parameter = null, bool clearNavigation = true);
}
