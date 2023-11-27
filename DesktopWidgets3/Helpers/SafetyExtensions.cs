using Microsoft.Extensions.Logging;

namespace DesktopWidgets3.Helpers;

public class SafetyExtensions
{
    public static T? IgnoreExceptions<T>(Func<T> action, ILogger? logger = null)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            logger?.LogInformation(ex, ex.Message);

            return default;
        }
    }
}
