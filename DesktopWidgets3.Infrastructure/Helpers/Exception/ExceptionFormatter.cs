using System.Text;

namespace DesktopWidgets3.Infrastructure.Helpers.Exception;

public class ExceptionFormatter
{
    public static string FormatExcpetion(System.Exception? exception)
    {
        StringBuilder formattedException = new()
        {
            Capacity = 200
        };

        formattedException.AppendLine("--------- UNHANDLED EXCEPTION ---------");

        if (exception is not null)
        {
            formattedException.AppendLine($">>>> HRESULT: {exception.HResult}");

            if (exception.Message is not null)
            {
                formattedException.AppendLine("--- MESSAGE ---");
                formattedException.AppendLine(exception.Message);
            }
            if (exception.StackTrace is not null)
            {
                formattedException.AppendLine("--- STACKTRACE ---");
                formattedException.AppendLine(exception.StackTrace);
            }
            if (exception.Source is not null)
            {
                formattedException.AppendLine("--- SOURCE ---");
                formattedException.AppendLine(exception.Source);
            }
            if (exception.InnerException is not null)
            {
                formattedException.AppendLine("--- INNER ---");
                formattedException.AppendLine(exception.InnerException.ToString());
            }
        }
        else
        {
            formattedException.AppendLine("Exception data is not available.");
        }

        formattedException.AppendLine("---------------------------------------");

        return formattedException.ToString();
    }
}
