using System.Collections.Specialized;
using System.Web;

using Microsoft.Windows.AppNotifications;

namespace DesktopWidgets3.Services;

internal class AppNotificationService(INavigationService navigationService) : IAppNotificationService
{
    private readonly INavigationService _navigationService = navigationService;

    ~AppNotificationService()
    {
        Unregister();
    }

    public void Initialize()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;

        AppNotificationManager.Default.Register();
    }

    // Handle notification invocations when your app is already running based on the notification arguments.
    public void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        /*switch (ParseArguments(args.Argument)["action"])
        {
            case "Dashboard":
                App.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    _navigationService.NavigateTo(typeof(DashboardViewModel).FullName!);
                    App.ShowMainWindow(true);
                });
                break;
        }*/
    }

    public bool Show(string payload)
    {
        var appNotification = new AppNotification(payload);

        AppNotificationManager.Default.Show(appNotification);

        return appNotification.Id != 0;
    }

    public NameValueCollection ParseArguments(string arguments)
    {
        return HttpUtility.ParseQueryString(arguments);
    }

    public void Unregister()
    {
        AppNotificationManager.Default.Unregister();
    }
}
