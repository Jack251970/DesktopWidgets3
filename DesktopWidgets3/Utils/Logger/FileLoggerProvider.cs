using Microsoft.Extensions.Logging;
using InfoHelper = DesktopWidgets3.Helpers.InfoHelper;

namespace DesktopWidgets3.Utils.Logger;

public sealed class FileLoggerProvider() : ILoggerProvider
{
    private readonly string logDirectory = 
        Path.Combine(LocalSettingsHelper.ApplicationDataPath, Constant.LogsFolder, InfoHelper.GetVersion().ToString());

    public ILogger CreateLogger(string categoryName)
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var logger = new FileLogger(logDirectory);
        /*_ = Task.Run(() => logger.PurgeLogs(100));*/
        return logger;
    }

    public void Dispose()
    {
    }
}
