// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace DesktopWidgets3.Infrastructure.Extensions;

#pragma warning disable CA2254 // Template should be a static expression

/// <summary>
/// Provide static extension for logging.
/// Edit from: Microsoft.Extensions.Logging.LoggerExtensions.
/// </summary>
public class LogExtensions
{
    public static ILogger? Logger => logger;
    private static ILogger? logger = null;

    public static void Initialize(ILogger logger)
    {
        LogExtensions.logger = logger;
    }

    private static string GetFullMessage(string? message, string className, string methodName)
    {
        return string.IsNullOrEmpty(className) ? $"{methodName}|{message}" : $"{className}.{methodName}|{message}";
    }

    //------------------------------------------DEBUG------------------------------------------//

    public static void LogDebug(string className, EventId eventId, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Debug, eventId, exception, message, args);
    }

    public static void LogDebug(string className, EventId eventId, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Debug, eventId, message, args);
    }

    public static void LogDebug(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Debug, exception, message, args);
    }

    public static void LogDebug(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Debug, message, args);
    }

    //------------------------------------------TRACE------------------------------------------//

    public static void LogTrace(string className, EventId eventId, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Trace, eventId, exception, message, args);
    }

    public static void LogTrace(string className, EventId eventId, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Trace, eventId, message, args);
    }

    public static void LogTrace(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Trace, exception, message, args);
    }

    public static void LogTrace(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Trace, message, args);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    public static void LogInformation(string className, EventId eventId, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Information, eventId, exception, message, args);
    }

    public static void LogInformation(string className, EventId eventId, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Information, eventId, message, args);
    }

    public static void LogInformation(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Information, exception, message, args);
    }

    public static void LogInformation(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Information, message, args);
    }

    //------------------------------------------WARNING------------------------------------------//

    public static void LogWarning(string className, EventId eventId, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Warning, eventId, exception, message, args);
    }

    public static void LogWarning(string className, EventId eventId, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Warning, eventId, message, args);
    }

    public static void LogWarning(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Warning, exception, message, args);
    }

    public static void LogWarning(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Warning, message, args);
    }

    //------------------------------------------ERROR------------------------------------------//

    public static void LogError(string className, EventId eventId, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Error, eventId, exception, message, args);
    }

    public static void LogError(string className, EventId eventId, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Error, eventId, message, args);
    }

    public static void LogError(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Error, exception, message, args);
    }

    public static void LogError(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Error, message, args);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    public static void LogCritical(string className, EventId eventId, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Critical, eventId, exception, message, args);
    }

    public static void LogCritical(string className, EventId eventId, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Critical, eventId, message, args);
    }

    public static void LogCritical(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Critical, exception, message, args);
    }

    public static void LogCritical(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        message = GetFullMessage(message, className, methodName);
        logger?.Log(LogLevel.Critical, message, args);
    }
}
