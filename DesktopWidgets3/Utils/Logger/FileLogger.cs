using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DesktopWidgets3.Utils.Logger;

public sealed class FileLogger(string logDirectory) : ILogger
{
    private readonly SemaphoreSlim semaphoreSlim = new(1);
    private string FilePath => Path.Combine(logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">Id of the event.</param>
    /// <param name="state">The entry to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter is null)
        {
            return;
        }

        await semaphoreSlim.WaitAsync();

        try
        {
            var message = formatter(state, exception);

            if (exception is not null && ExceptionFormatter.FormatExcpetion(exception) is string str && (!string.IsNullOrEmpty(str)))
            {
                message = $"{message}" + Environment.NewLine + $"{str}";
            }

            await File.AppendAllTextAsync(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}|{logLevel}|{message}" + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Writing to log file failed with the following exception:\n{e}");
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// Checks if the given <paramref name="logLevel"/> is enabled.
    /// </summary>
    /// <param name="logLevel">Level to be checked.</param>
    /// <returns><c>true</c> if enabled.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
#if DEBUG
        return true;
#else
        return logLevel >= LogLevel.Information;
#endif
    }

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <param name="state">The identifier for the scope.</param>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose.</returns>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <summary>
    /// Purge lines in the log file.
    /// </summary>
    /// <param name="numberOfLinesKept">
    /// Number of lines to keep.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task PurgeLogs(int numberOfLinesKept)
    {
        if (!File.Exists(FilePath))
        {
            return;
        }

        await semaphoreSlim.WaitAsync();

        try
        {
            var lines = await File.ReadAllLinesAsync(FilePath);
            if (lines.Length > numberOfLinesKept)
            {
                var lastLines = lines.Skip(Math.Max(0, lines.Length - numberOfLinesKept));
                await File.WriteAllLinesAsync(FilePath, lastLines);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Purging the log file failed with the following exception:\n{e}");
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }
}
