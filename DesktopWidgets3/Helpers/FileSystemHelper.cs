using System.Diagnostics;

namespace DesktopWidgets3.Helpers;

public class FileSystemHelper
{
    public static void OpenInExplorer(string path) => OpenInExplorer(path, string.Empty);

    public static void OpenInExplorer(string path, string args)
    {
        var process = new Process();
        process.StartInfo.FileName = "explorer.exe";
        process.StartInfo.Arguments = args + " \"" + path + "\"";
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.Verb = "open";
        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        process.Start();
    }
}
