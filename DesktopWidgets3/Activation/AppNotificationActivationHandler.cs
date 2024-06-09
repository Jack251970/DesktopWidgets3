using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace DesktopWidgets3.Activation;

internal class AppNotificationActivationHandler(INavigationService navigationService, IAppNotificationService notificationService) : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService = navigationService;
    private readonly IAppNotificationService _notificationService = notificationService;

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        return AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind == ExtendedActivationKind.AppNotification;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // Handle notification activations here.

        //// // Access the AppNotificationActivatedEventArgs.
        //// var activatedEventArgs = (AppNotificationActivatedEventArgs)AppInstance.GetCurrent().GetActivatedEventArgs().Data;

        //// // Navigate to a specific page based on the notification arguments.
        //// if (_notificationService.ParseArguments(activatedEventArgs.Argument)["action"] == "Settings")
        //// {
        ////     // Queue navigation with low priority to allow the UI to initialize.
        ////     App.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        ////     {
        ////         _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        ////     });
        //// }

        //// App.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        //// {
        ////         App.MainWindow.ShowMessageDialogAsync("TODO: Handle notification activations.", "Notification Activation");
        //// });*/

        await Task.CompletedTask;
    }
}
