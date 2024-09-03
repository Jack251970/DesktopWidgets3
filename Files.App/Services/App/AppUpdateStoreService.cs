// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Net.Http;
using Windows.Foundation.Metadata;
using Windows.Services.Store;
using Windows.Storage;
using WinRT.Interop;

namespace Files.App.Services;

internal sealed class StoreUpdateService : ObservableObject, IUpdateService
{
	private StoreContext? _storeContext;
    private List<StorePackageUpdate>? _updatePackages;

    private bool IsMandatory => _updatePackages?.Where(e => e.Mandatory).ToList().Count >= 1;

	private bool _isUpdateAvailable;
	public bool IsUpdateAvailable
	{
		get => _isUpdateAvailable;
		set => SetProperty(ref _isUpdateAvailable, value);
	}

	private bool _isUpdating;
	public bool IsUpdating
	{
		get => _isUpdating;
		private set => SetProperty(ref _isUpdating, value);
	}

	private bool _isReleaseNotesAvailable;
	public bool IsReleaseNotesAvailable
	{
		get => _isReleaseNotesAvailable;
		private set => SetProperty(ref _isReleaseNotesAvailable, value);
	}

    // CHANGE: Remove SystemInformation.
    public bool IsAppUpdated => false;// SystemInformation.Instance.IsAppUpdated;

    public StoreUpdateService()
	{
		_updatePackages = [];
    }

	public async Task DownloadUpdatesAsync(IFolderViewViewModel folderViewViewModel)
	{
		OnUpdateInProgress();

		if (!HasUpdates())
		{
			return;
		}

		// double check for Mandatory
		if (IsMandatory)
		{
			// Show dialog
			var dialog = await ShowDialogAsync(folderViewViewModel);
			if (!dialog)
			{
				// User rejected mandatory update.
				OnUpdateCancelled();
				return;
			}
		}

		await DownloadAndInstallAsync();
		OnUpdateCompleted();
	}

	public async Task DownloadMandatoryUpdatesAsync(IFolderViewViewModel folderViewViewModel)
	{
		// Prompt the user to download if the package list
		// contains mandatory updates.
		if (IsMandatory && HasUpdates())
		{
			if (await ShowDialogAsync(folderViewViewModel))
			{
				LogExtensions.LogInformation("STORE: Downloading updates...");
				OnUpdateInProgress();
				await DownloadAndInstallAsync();
				OnUpdateCompleted();
			}
		}
	}

	public async Task CheckForUpdatesAsync(IFolderViewViewModel folderViewViewModel)
	{
		IsUpdateAvailable = false;
		LogExtensions.LogInformation("STORE: Checking for updates...");

		await GetUpdatePackagesAsync(folderViewViewModel);

		if (_updatePackages is not null && _updatePackages.Count > 0)
		{
			LogExtensions.LogInformation("STORE: Update found.");
			IsUpdateAvailable = true;
		}
	}

	private async Task DownloadAndInstallAsync()
	{
        // Save the updated tab list before installing the update
        AppLifecycleHelper.SaveSessionTabs();

        App.AppModel.ForceProcessTermination = true;

        var downloadOperation = _storeContext?.RequestDownloadAndInstallStorePackageUpdatesAsync(_updatePackages);
		var result = await downloadOperation.AsTask();

		if (result.OverallState == StorePackageUpdateState.Canceled)
        {
            App.AppModel.ForceProcessTermination = false;
        }
    }

	private async Task GetUpdatePackagesAsync(IFolderViewViewModel folderViewViewModel)
	{
		try
		{
			_storeContext ??= await Task.Run(StoreContext.GetDefault);

			InitializeWithWindow.Initialize(_storeContext, folderViewViewModel.WindowHandle);

			var updateList = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
			_updatePackages = updateList?.ToList();
		}
		catch (FileNotFoundException)
		{
			// Suppress the FileNotFoundException.
			// GetAppAndOptionalStorePackageUpdatesAsync throws for unknown reasons.
		}
	}

	private static async Task<bool> ShowDialogAsync(IFolderViewViewModel folderViewViewModel)
	{
		// FILESTODO: Use IDialogService in future.
		ContentDialog dialog = new()
		{
			Title = "ConsentDialogTitle".GetLocalizedResource(),
			Content = "ConsentDialogContent".GetLocalizedResource(),
			CloseButtonText = "Close".GetLocalizedResource(),
			PrimaryButtonText = "ConsentDialogPrimaryButtonText".GetLocalizedResource()
		};

		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            dialog.XamlRoot = folderViewViewModel.XamlRoot;
        }

        var result = await dialog.TryShowAsync(folderViewViewModel);

		return result == ContentDialogResult.Primary;
	}

	public async Task CheckLatestReleaseNotesAsync(CancellationToken cancellationToken = default)
	{
		if (!IsAppUpdated)
        {
            return;
        }

        var result = await GetLatestReleaseNotesAsync(cancellationToken);

		if (result is not null)
        {
            IsReleaseNotesAvailable = true;
        }
    }

	public async Task<string?> GetLatestReleaseNotesAsync(CancellationToken cancellationToken = default)
	{
        // CHANGE: Remove SystemInformation.
        /*var applicationVersion = $"{SystemInformation.Instance.ApplicationVersion.Major}.{SystemInformation.Instance.ApplicationVersion.Minor}.{SystemInformation.Instance.ApplicationVersion.Build}";*/
        var applicationVersion = InfoHelper.GetVersion().ToString();
        var releaseNotesLocation = string.Concat("https://raw.githubusercontent.com/files-community/Release-Notes/main/", applicationVersion, ".md");

		using var client = new HttpClient();

		try
		{
			var result = await client.GetStringAsync(releaseNotesLocation, cancellationToken);
			return result == string.Empty ? null : result;
		}
		catch
		{
			return null;
		}
	}

	public async Task CheckAndUpdateFilesLauncherAsync()
	{
		var destFolderPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, "Files");
		var destExeFilePath = Path.Combine(destFolderPath, "Files.App.Launcher.exe");

		if (Path.Exists(destExeFilePath))
		{
			var hashEqual = false;
			var srcHashFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Files.App/Assets/FilesOpenDialog/Files.App.Launcher.exe.sha256"));
			var destHashFilePath = Path.Combine(destFolderPath, "Files.App.Launcher.exe.sha256");

			if (Path.Exists(destHashFilePath))
			{
				using var srcStream = (await srcHashFile.OpenReadAsync()).AsStream();
				using var destStream = File.OpenRead(destHashFilePath);

				hashEqual = HashEqual(srcStream, destStream);
			}

			if (!hashEqual)
			{
				var srcExeFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Files.App/Assets/FilesOpenDialog/Files.App.Launcher.exe"));
				var destFolder = await StorageFolder.GetFolderFromPathAsync(destFolderPath);

				await srcExeFile.CopyAsync(destFolder, "Files.App.Launcher.exe", NameCollisionOption.ReplaceExisting);
				await srcHashFile.CopyAsync(destFolder, "Files.App.Launcher.exe.sha256", NameCollisionOption.ReplaceExisting);

				LogExtensions.LogInformation("Files.App.Launcher updated.");
			}
		}

        static bool HashEqual(Stream a, Stream b)
		{
			Span<byte> bufferA = stackalloc byte[64];
			Span<byte> bufferB = stackalloc byte[64];

			a.Read(bufferA);
			b.Read(bufferB);

			return bufferA.SequenceEqual(bufferB);
		}
	}

	private bool HasUpdates()
	{
		return _updatePackages is not null && _updatePackages.Count >= 1;
	}

	private void OnUpdateInProgress()
	{
		IsUpdating = true;
	}

	private void OnUpdateCompleted()
	{
		IsUpdating = false;
		IsUpdateAvailable = false;

		_updatePackages?.Clear();
	}

	private void OnUpdateCancelled()
	{
		IsUpdating = false;
	}
}


