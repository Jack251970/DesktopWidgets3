using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;

namespace DesktopWidgets3.ViewModels.Pages.Widget;

public partial class FrameShellViewModel : ObservableRecipient
{
    public IWidgetNavigationService WidgetNavigationService
    {
        get;
    }

    public FrameShellViewModel(IWidgetNavigationService widgetNavigationService)
    {
        WidgetNavigationService = widgetNavigationService;
    }
}
