// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers;

public static class Win32Helpers
{
	public static async Task<bool> InvokeWin32ComponentAsync(string applicationPath, IFolderViewViewModel viewModel, string arguments = null!, bool runAsAdmin = false, string workingDirectory = null!)
	{
		return await InvokeWin32ComponentsAsync(applicationPath.CreateEnumerable(), viewModel, arguments, runAsAdmin, workingDirectory);
	}

	public static async Task<bool> InvokeWin32ComponentsAsync(IEnumerable<string> applicationPaths, IFolderViewViewModel viewModel, string arguments = null!, bool runAsAdmin = false, string workingDirectory = null!)
	{
		Debug.WriteLine("Launching EXE in FullTrustProcess");

		if (string.IsNullOrEmpty(workingDirectory))
		{
			workingDirectory = viewModel.WorkingDirectory; // TODO: change to associatedInstance.FilesystemViewModel.WorkingDirectory;
        }

		var application = applicationPaths.FirstOrDefault();
		if (string.IsNullOrEmpty(workingDirectory))
		{
			workingDirectory = viewModel.WorkingDirectory;
		}

		if (runAsAdmin)
		{
			return await LaunchHelper.LaunchAppAsync(application!, "RunAs", workingDirectory);
		}
		else
		{
			return await LaunchHelper.LaunchAppAsync(application!, arguments, workingDirectory);
		}
	}
}