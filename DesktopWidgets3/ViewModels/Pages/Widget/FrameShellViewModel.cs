using CommunityToolkit.Mvvm.ComponentModel;

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
