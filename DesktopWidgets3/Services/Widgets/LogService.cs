using System.Runtime.CompilerServices;
using Serilog;

namespace DesktopWidgets3.Services.Widgets;

internal class LogService : ILogService
{
    //------------------------------------------TRACE------------------------------------------//

    public void LogTrace(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Verbose(exception, message ?? string.Empty, args);
    }

    public void LogTrace(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Verbose(message ?? string.Empty, args);
    }

    //------------------------------------------DEBUG------------------------------------------//

    public void LogDebug(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Debug(exception, message ?? string.Empty, args);
    }

    public void LogDebug(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Debug(message ?? string.Empty, args);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    public void LogInformation(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Information(exception, message ?? string.Empty, args);
    }

    public void LogInformation(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Information(message ?? string.Empty, args);
    }

    //------------------------------------------WARNING------------------------------------------//

    public void LogWarning(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Warning(exception, message ?? string.Empty, args);
    }

    public void LogWarning(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Warning(message ?? string.Empty, args);
    }

    //------------------------------------------ERROR------------------------------------------//

    public void LogError(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Error(exception, message ?? string.Empty, args);
    }

    /// <summary>
    /// Log error message
    /// </summary>
    public void LogError(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Error(message ?? string.Empty, args);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    public void LogCritical(string className, Exception? exception, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Fatal(exception, message ?? string.Empty, args);
    }

    public void LogCritical(string className, string? message, [CallerMemberName] string methodName = "", params object?[] args)
    {
        var log = Log.ForContext("SourceContext", $"{className}|{methodName}");
        log.Fatal(message ?? string.Empty, args);
    }
}
