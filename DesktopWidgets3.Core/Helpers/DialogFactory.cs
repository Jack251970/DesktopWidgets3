using WinUIEx;

namespace DesktopWidgets3.Core.Helpers;

public static class DialogFactory
{
    public static async Task<WidgetDialogResult> ShowDeleteWidgetDialogAsync(WindowEx? window = null)
    {
        var title = "Dialog_DeleteWidget.Title".GetLocalizedString();
        var content = "Dialog_DeleteWidget.Content".GetLocalizedString();
        return await DependencyExtensions.GetRequiredService<IDialogService>().ShowTwoButtonDialogAsync(window, title, content);
    }

    public static async Task ShowDeleteWidgetFullScreenDialogAsync(Func<Task> deleteFunc)
    {
        var title = "Dialog_DeleteWidget.Title".GetLocalizedString();
        var content = "Dialog_DeleteWidget.Content".GetLocalizedString();
        await DependencyExtensions.GetRequiredService<IDialogService>().ShowFullScreenTwoButtonDialogAsync(title, content, func: async (result) =>
        {
            if (result == WidgetDialogResult.Left)
            {
                await deleteFunc();
            }
        });
    }

    public static async Task<WidgetDialogResult> ShowQuitEditModeDialogAsync(WindowEx? window = null)
    {
        var title = "Dialog_SaveCurrentLayout.Title".GetLocalizedString();
        var content = "Dialog_SaveCurrentLayout.Content".GetLocalizedString();
        return await DependencyExtensions.GetRequiredService<IDialogService>().ShowTwoButtonDialogAsync(window, title, content);
    }

    public static async Task ShowCreateWidgetErrorDialogAsync(WindowEx? window = null)
    {
        var title = string.Empty;
        var content = "CouldNotCreateWidgetError".GetLocalizedString(Constants.DevHomeDashboard);
        await DependencyExtensions.GetRequiredService<IDialogService>().ShowOneButtonDialogAsync(window, title, content);
    }

    public static async Task<WidgetDialogResult> ShowRestartApplicationDialogAsync(WindowEx? window = null)
    {
        var title = "RestartApplication.Title".GetLocalizedString();
        var content = "RestartApplication.Content".GetLocalizedString();
        return await DependencyExtensions.GetRequiredService<IDialogService>().ShowTwoButtonDialogAsync(window, title, content);
    }

    public static async Task ShowRunningAsAdminMessageDialogAsync(WindowEx? window = null)
    {
        var title = string.Empty;
        var content = "RunningAsAdminMessage.Text".GetLocalizedString(Constants.DevHomeDashboard);
        await DependencyExtensions.GetRequiredService<IDialogService>().ShowOneButtonDialogAsync(window, title, content);
    }

    public static async Task ShowRestartDevHomeMessageDialogAsync(WindowEx? window = null)
    {
        var title = string.Empty;
        var content = "RestartDevHomeMessage.Text".GetLocalizedString(Constants.DevHomeDashboard);
        await DependencyExtensions.GetRequiredService<IDialogService>().ShowOneButtonDialogAsync(window, title, content);
    }

    public static async Task ShowUpdatingWidgetServiceMessageDialogAsync(WindowEx? window = null)
    {
        var title = string.Empty;
        var content = "UpdatingWidgetServiceMessage.Text".GetLocalizedString(Constants.DevHomeDashboard);
        await DependencyExtensions.GetRequiredService<IDialogService>().ShowOneButtonDialogAsync(window, title, content);
    }

    public static async Task ShowNotOKServiceMessageDialogAsync(WindowEx? window = null)
    {
        var title = string.Empty;
        var content = "NotOKWidgetServiceMessage.Text".GetLocalizedString(Constants.DevHomeDashboard);
        await DependencyExtensions.GetRequiredService<IDialogService>().ShowOneButtonDialogAsync(window, title, content);
    }

    public static async Task ShowUpdateWidgetsMessageDialogAsync(WindowEx? window = null)
    {
        var title = string.Empty;
        var content = "UpdateWidgetsMessage.Text".GetLocalizedString(Constants.DevHomeDashboard);
        await DependencyExtensions.GetRequiredService<IDialogService>().ShowOneButtonDialogAsync(window, title, content);
    }
}
