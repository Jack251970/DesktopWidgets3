// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;

namespace Files.App.Helpers;

public static class Win32Helpers
{
    public static async Task<bool> InvokeWin32ComponentAsync(string applicationPath, string arguments, string workingDirectory, bool runAsAdmin = false)
    {
        if (runAsAdmin)
        {
            return await LaunchHelper.LaunchAppAsync(applicationPath, "RunAs", workingDirectory);
        }
        else
        {
            return await LaunchHelper.LaunchAppAsync(applicationPath, arguments, workingDirectory);
        }
    }
}
