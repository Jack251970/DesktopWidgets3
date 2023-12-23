// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Utils.Shell;
using Files.Shared.Extensions;

namespace Files.App.Helpers;

public static class Win32Helpers
{
    public static async Task<bool> InvokeWin32ComponentAsync(string applicationPath, FolderViewViewModel viewModel, string arguments = null!, bool runAsAdmin = false, string workingDirectory = null!)
    {
        return await InvokeWin32ComponentsAsync(applicationPath.CreateEnumerable(), viewModel, arguments, runAsAdmin, workingDirectory);
    }

    public static async Task<bool> InvokeWin32ComponentsAsync(IEnumerable<string> applicationPaths, FolderViewViewModel viewModel, string arguments = null!, bool runAsAdmin = false, string workingDirectory = null!)
    {
        if (string.IsNullOrEmpty(workingDirectory))
        {
            workingDirectory = viewModel.ItemViewModel.WorkingDirectory;
        }

        var application = applicationPaths.FirstOrDefault()!;
        if (string.IsNullOrEmpty(workingDirectory))
        {
            workingDirectory = viewModel.ItemViewModel.WorkingDirectory;
        }

        if (runAsAdmin)
        {
            return await LaunchHelper.LaunchAppAsync(application, "RunAs", workingDirectory);
        }
        else
        {
            return await LaunchHelper.LaunchAppAsync(application, arguments, workingDirectory);
        }
    }

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
