// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text.RegularExpressions;
using Files.App.Utils.Shell;
using Files.App.Utils.Storage;
using Files.Core.Data.Items;

namespace Files.App.Utils.RecycleBin;

public static partial class RecycleBinHelpers
{
    private static readonly Regex recycleBinPathRegex = MyRegex();

    public static async Task<List<ShellFileItem>> EnumerateRecycleBin()
    {
        return (await Win32Shell.GetShellFolderAsync(Constants.UserEnvironmentPaths.RecycleBinPath, "Enumerate", 0, int.MaxValue)).Enumerate;
    }

    public static bool IsPathUnderRecycleBin(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && recycleBinPathRegex.IsMatch(path);
    }

    public static async Task<bool> HasRecycleBin(string? path)
    {
        if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return false;
        }

        var result = await FileOperationsHelpers.TestRecycleAsync(path.Split('|'));

        return result.Item1 &= result.Item2 is not null && result.Item2.Items.All(x => x.Succeeded);
    }

    [GeneratedRegex("^[A-Z]:\\\\\\$Recycle\\.Bin\\\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
