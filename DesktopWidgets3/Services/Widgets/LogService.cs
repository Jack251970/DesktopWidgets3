using System.Runtime.CompilerServices;

namespace DesktopWidgets3.Services.Widgets;

internal class LogService : ILogService
{
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
}
