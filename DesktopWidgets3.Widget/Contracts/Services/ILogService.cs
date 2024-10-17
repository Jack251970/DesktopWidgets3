using System.Runtime.CompilerServices;

namespace DesktopWidgets3.Widget;

public interface ILogService
{
    /// <summary>
    /// Log trace message
    /// Message will only be logged in Debug mode
    /// </summary>
    void LogTrace(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log trace message
    /// Message will only be logged in Debug mode
    /// </summary>
    void LogTrace(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log debug message
    /// Message will only be logged in Debug mode
    /// </summary>
    void LogDebug(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log debug message
    /// Message will only be logged in Debug mode
    /// </summary>
    void LogDebug(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log information message
    /// </summary>
    void LogInformation(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log information message
    /// </summary>
    void LogInformation(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log warning message
    /// </summary>
    void LogWarning(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log warning message
    /// </summary>
    void LogWarning(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log error message
    /// </summary>
    void LogError(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log error message
    /// </summary>
    void LogError(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log critical message
    /// Will throw if in debug mode so developer will be aware, otherwise logs the eror message.
    /// </summary>
    void LogCritical(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args);

    /// <summary>
    /// Log critical message
    /// Will throw if in debug mode so developer will be aware, otherwise logs the eror message.
    /// </summary>
    void LogCritical(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args);
}
