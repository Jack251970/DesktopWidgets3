using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace DesktopWidgets3.Helpers;

/// <summary>
/// Provides static extension for shell folders.
/// https://github.com/files-community/Files/blob/main/src/Files.App/Utils/Shell/ShellFolderExtensions.cs
/// </summary>
public static class ShellFolderExtensions
{
    public static bool GetStringAsPIDL(string pathOrPIDL, out Shell32.PIDL pPIDL)
    {
        if (pathOrPIDL.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
        {
            pPIDL = pathOrPIDL.Replace(@"\\SHELL\", "", StringComparison.Ordinal)
                // Avoid confusion with path separator
                .Replace("_", "/")
                .Split('\\', StringSplitOptions.RemoveEmptyEntries)
                .Select(pathSegment => new Shell32.PIDL(Convert.FromBase64String(pathSegment)))
                .Aggregate(Shell32.PIDL.Combine);

            return true;
        }
        else
        {
            pPIDL = Shell32.PIDL.Null;

            return false;
        }
    }

    public static ShellItem GetShellItemFromPathOrPIDL(string pathOrPIDL)
    {
        return GetStringAsPIDL(pathOrPIDL, out var pPIDL) ? ShellItem.Open(pPIDL) : ShellItem.Open(pathOrPIDL);
    }
}
