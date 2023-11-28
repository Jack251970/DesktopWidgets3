using Files.App.Utils.Shell;
using Files.Core.Data.Items;
using Files.Shared.Helpers;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Utils.Storage;

public class FileOperationsHelpers
{
    public static async Task<ShellLinkItem?> ParseLinkAsync(string linkPath)
    {
        if (string.IsNullOrEmpty(linkPath))
        {
            return null;
        }

        var targetPath = string.Empty;

        try
        {
            if (FileExtensionHelpers.IsShortcutFile(linkPath))
            {
                using var link = new ShellLink(linkPath, LinkResolution.NoUIWithMsgPump, default, TimeSpan.FromMilliseconds(100));
                targetPath = link.TargetPath;
                return ShellFolderExtensions.GetShellLinkItem(link);
            }
            else if (FileExtensionHelpers.IsWebLinkFile(linkPath))
            {
                targetPath = await Win32API.StartSTATask(() =>
                {
                    var ipf = new Url.IUniformResourceLocator();
                    ((System.Runtime.InteropServices.ComTypes.IPersistFile)ipf).Load(linkPath, 0);
                    ipf.GetUrl(out var retVal);
                    return retVal;
                });
                return string.IsNullOrEmpty(targetPath) ?
                    new ShellLinkItem
                    {
                        TargetPath = string.Empty,
                        InvalidTarget = true
                    } : new ShellLinkItem { TargetPath = targetPath };
            }
            return null;
        }
        catch (FileNotFoundException) // Could not parse shortcut
        {
            // Return a item containing the invalid target path
            return new ShellLinkItem
            {
                TargetPath = string.IsNullOrEmpty(targetPath) ? string.Empty : targetPath,
                InvalidTarget = true
            };
        }
        catch (Exception)
        {
            return null;
        }
    }
}
