using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace DesktopWidgets3.Core.Extensions;

#pragma warning disable CA2254 // Template should be a static expression

/// <summary>
/// Provide static extension for logging.
/// </summary>
public class LogExtensions
{
    public static ILogger? Logger => logger;
    private static ILogger? logger = null!;

    public static void Initialize(ILogger logger)
    {
        LogExtensions.logger = logger;
    }

    private static string GetFullMessage(string? message, string className, string methodName)
    {
        return string.IsNullOrEmpty(className) ? $"{methodName}|{message}" : $"{className}.{methodName}|{message}";
    }

    //------------------------------------------DEBUG------------------------------------------//

    public static void LogDebug(EventId eventId, Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Debug, eventId, exception, message, args);
    }

    public static void LogDebug(EventId eventId, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Debug, eventId, message, args);
    }

    public static void LogDebug(Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Debug, exception, message, args);
    }

    public static void LogDebug(string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Debug, message, args);
    }

    //------------------------------------------TRACE------------------------------------------//

    public static void LogTrace(EventId eventId, Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Trace, eventId, exception, message, args);
    }

    public static void LogTrace(EventId eventId, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Trace, eventId, message, args);
    }

    public static void LogTrace(Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Trace, exception, message, args);
    }

    public static void LogTrace(string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Trace, message, args);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    public static void LogInformation(EventId eventId, Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Information, eventId, exception, message, args);
    }

    public static void LogInformation(EventId eventId, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Information, eventId, message, args);
    }

    public static void LogInformation(Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Information, exception, message, args);
    }

    public static void LogInformation(string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Information, message, args);
    }

    //------------------------------------------WARNING------------------------------------------//

    public static void LogWarning(EventId eventId, Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Warning, eventId, exception, message, args);
    }

    public static void LogWarning(EventId eventId, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Warning, eventId, message, args);
    }

    public static void LogWarning(Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Warning, exception, message, args);
    }

    public static void LogWarning(string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Warning, message, args);
    }

    //------------------------------------------ERROR------------------------------------------//

    public static void LogError(EventId eventId, Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Error, eventId, exception, message, args);
    }

    public static void LogError(EventId eventId, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Error, eventId, message, args);
    }

    public static void LogError(Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Error, exception, message, args);
    }

    public static void LogError(string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Error, message, args);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    public static void LogCritical(EventId eventId, Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Critical, eventId, exception, message, args);
    }

    public static void LogCritical(EventId eventId, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Critical, eventId, message, args);
    }

    public static void LogCritical(Exception? exception, string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Critical, exception, message, args);
    }

    public static void LogCritical(string? message, string className = "", [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Critical, message, args);
    }
}
