﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Win32;

namespace Files.App.Helpers;

internal static class SoftwareHelpers
{
	public static bool IsVSCodeInstalled()
	{
		var registryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
		var vsCodeName = "Microsoft Visual Studio Code";

		return
			ContainsName(Registry.CurrentUser.OpenSubKey(registryKey), vsCodeName) ||
			ContainsName(Registry.LocalMachine.OpenSubKey(registryKey), vsCodeName);
	}

	public static bool IsVSInstalled()
	{
		var registryKey = @"SOFTWARE\Microsoft\VisualStudio";

		var key = Registry.LocalMachine.OpenSubKey(registryKey);
		if (key is null)
        {
            return false;
        }

        key.Close();

		return true;
	}

	public static bool IsPythonInstalled()
	{
		try
		{
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            using var reader = process!.StandardOutput;
            var result = reader.ReadToEnd();
            return result.Contains("Python");
        }
		catch
		{
			return false;
		}
	}

	private static bool ContainsName(RegistryKey? key, string find)
	{
		if (key is null)
        {
            return false;
        }

        foreach (var subKey in key.GetSubKeyNames().Select(key.OpenSubKey))
		{
			var displayName = subKey?.GetValue("DisplayName") as string;
			if (!string.IsNullOrWhiteSpace(displayName) && displayName.StartsWith(find))
			{
				key.Close();

				return true;
			}
		}

		key.Close();

		return false;
	}
}
