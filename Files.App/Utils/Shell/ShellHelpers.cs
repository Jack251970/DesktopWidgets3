// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Utils.Shell;

public static class ShellHelpers
{
	public static string ResolveShellPath(string shPath)
	{
		if (shPath.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
        {
            return Constants.UserEnvironmentPaths.RecycleBinPath;
        }

        if (shPath.StartsWith(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
        {
            return Constants.UserEnvironmentPaths.MyComputerPath;
        }

        if (shPath.StartsWith(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return Constants.UserEnvironmentPaths.NetworkFolderPath;
        }

        return shPath;
	}

	public static string GetShellNameFromPath(string shPath)
	{
		return shPath switch
		{
			"Home" => "Home".ToLocalized(),
			Constants.UserEnvironmentPaths.RecycleBinPath => "RecycleBin".ToLocalized(),
			Constants.UserEnvironmentPaths.NetworkFolderPath => "SidebarNetworkDrives".ToLocalized(),
			Constants.UserEnvironmentPaths.MyComputerPath => "ThisPC".ToLocalized(),
			_ => shPath
		};
	}

	public static string GetLibraryFullPathFromShell(string shPath)
	{
		var partialPath = shPath.Substring(shPath.IndexOf('\\') + 1);
		return Path.Combine(ShellLibraryItem.LibrariesPath, partialPath);
	}
}
