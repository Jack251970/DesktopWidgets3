using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services;

internal class PublicAPIService : IPublicAPIService
{
    private static IThemeSelectorService ThemeSelectorService => DependencyExtensions.GetRequiredService<IThemeSelectorService>();
    private static IWidgetManagerService WidgetManagerService => DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    #region theme

    public ElementTheme RootTheme => ThemeSelectorService.Theme;

    public Action<ElementTheme>? ElementTheme_Changed { get; set; }

    #endregion

    #region widget

    public async Task UpdateWidgetSettings(FrameworkElement element, BaseWidgetSettings settings, bool loadSettings)
    {
        var widgetId = WidgetProperties.GetId(element);
        var indexTag = WidgetProperties.GetIndexTag(element);
        await WidgetManagerService.UpdateWidgetSettingsAsync(widgetId, indexTag, settings, loadSettings);
    }

    public async Task UpdateWidgetSettings(BaseWidgetViewModel viewModel, BaseWidgetSettings settings, bool loadSettings)
    {
        if (viewModel.WidgetWindow is WidgetWindow widgetWindow)
        {
            await WidgetManagerService.UpdateWidgetSettingsAsync(widgetWindow.Id, widgetWindow.IndexTag, settings, loadSettings);
        }
    }

    #endregion

    #region log

    //------------------------------------------TRACE------------------------------------------//

    public void LogTrace(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogTrace(className, exception, message, methodName, args);
    }

    public void LogTrace(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogTrace(className, message, methodName, args);
    }

    //------------------------------------------DEBUG------------------------------------------//

    public void LogDebug(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogDebug(className, exception, message, methodName, args);
    }

    public void LogDebug(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogDebug(className, message, methodName, args);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    public void LogInformation(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogInformation(className, exception, message, methodName, args);
    }

    public void LogInformation(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogInformation(className, message, methodName, args);
    }

    //------------------------------------------WARNING------------------------------------------//

    public void LogWarning(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogWarning(className, exception, message, methodName, args);
    }

    public void LogWarning(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogWarning(className, message, methodName, args);
    }

    //------------------------------------------ERROR------------------------------------------//

    public void LogError(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogError(className, exception, message, methodName, args);
    }

    /// <summary>
    /// Log error message
    /// </summary>
    public void LogError(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogError(className, message, methodName, args);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    public void LogCritical(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogCritical(className, exception, message, methodName, args);
    }

    public void LogCritical(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        LogExtensions.LogCritical(className, message, methodName, args);
    }

    #endregion
}
