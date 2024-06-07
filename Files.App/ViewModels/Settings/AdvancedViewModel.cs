// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SevenZip;
using System.IO;
using System.Text;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Files.App.ViewModels.Settings;

public sealed class AdvancedViewModel : ObservableObject
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    private IUserSettingsService UserSettingsService { get; set; } = null!;

	private readonly IFileTagsSettingsService fileTagsSettingsService = DependencyExtensions.GetService<IFileTagsSettingsService>();

	public ICommand SetAsDefaultExplorerCommand { get; }
	public ICommand SetAsOpenFileDialogCommand { get; }
	public ICommand ExportSettingsCommand { get; }
	public ICommand ImportSettingsCommand { get; }
	public ICommand OpenSettingsJsonCommand { get; }
	public AsyncRelayCommand OpenFilesOnWindowsStartupCommand { get; }

	public AdvancedViewModel()
	{
		IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
		IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();

		SetAsDefaultExplorerCommand = new AsyncRelayCommand(SetAsDefaultExplorerAsync);
		SetAsOpenFileDialogCommand = new AsyncRelayCommand(SetAsOpenFileDialogAsync);
		ExportSettingsCommand = new AsyncRelayCommand(ExportSettingsAsync);
		ImportSettingsCommand = new AsyncRelayCommand(ImportSettingsAsync);
		OpenSettingsJsonCommand = new AsyncRelayCommand(OpenSettingsJsonAsync);
		OpenFilesOnWindowsStartupCommand = new AsyncRelayCommand(OpenFilesOnWindowsStartupAsync);

        // CHANGE: Remove startup task state detecting.
		/*_ = DetectOpenFilesAtStartupAsync();*/
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
        UserSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
    }

	private async Task OpenSettingsJsonAsync()
	{
		await SafetyExtensions.IgnoreExceptions(async () =>
		{
			var settingsJsonFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/user_settings.json"));
			if (!await Launcher.LaunchFileAsync(settingsJsonFile))
            {
                await ContextMenu.InvokeVerb("open", settingsJsonFile.Path);
            }
        });
	}

	private async Task SetAsDefaultExplorerAsync()
	{
		// Make sure IsSetAsDefaultFileManager is updated
		await Task.Yield();

		if (IsSetAsDefaultFileManager == DetectIsSetAsDefaultFileManager())
        {
            return;
        }

        var destFolder = Path.Combine(LocalSettingsExtensions.GetApplicationDataFolder("Files"), "FilesOpenDialog");
		Directory.CreateDirectory(destFolder);

		foreach (var file in Directory.GetFiles(Path.Combine(InfoHelper.GetInstalledLocation(), "Files.App", "Assets", "FilesOpenDialog")))
		{
			if (!SafetyExtensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), App.Logger))
			{
				// Error copying files
				await DetectResult();
				return;
			}
		}

		var dataPath = Environment.ExpandEnvironmentVariables("%LocalAppData%\\Files");
		if (IsSetAsDefaultFileManager)
		{
			if (!await Win32Helper.RunPowershellCommandAsync($"-command \"New-Item -Force -Path '{dataPath}' -ItemType Directory; Copy-Item -Filter *.* -Path '{destFolder}\\*' -Recurse -Force -Destination '{dataPath}'\"", false))
			{
				// Error copying files
				await DetectResult();
				return;
			}
		}
		else
		{
			await Win32Helper.RunPowershellCommandAsync($"-command \"Remove-Item -Path '{dataPath}' -Recurse -Force\"", false);
		}

		try
		{
			using var regProcess = Process.Start(new ProcessStartInfo("regedit.exe", @$"/s ""{Path.Combine(destFolder, IsSetAsDefaultFileManager ? "SetFilesAsDefault.reg" : "UnsetFilesAsDefault.reg")}""") { UseShellExecute = true, Verb = "runas" });
			if (regProcess is not null)
            {
                await regProcess.WaitForExitAsync();
            }
        }
		catch
		{
			// Canceled UAC
		}

		await DetectResult();
	}

	private Task DetectResult()
	{
		IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
		if (!IsSetAsDefaultFileManager)
		{
			IsSetAsOpenFileDialog = false;
			return SetAsOpenFileDialogAsync();
		}

		return Task.CompletedTask;
	}

	private async Task SetAsOpenFileDialogAsync()
	{
		// Make sure IsSetAsDefaultFileManager is updated
		await Task.Yield();
		if (IsSetAsOpenFileDialog == DetectIsSetAsOpenFileDialog())
        {
            return;
        }

        var destFolder = Path.Combine(LocalSettingsExtensions.GetApplicationDataFolder("Files"), "FilesOpenDialog");
		Directory.CreateDirectory(destFolder);
		foreach (var file in Directory.GetFiles(Path.Combine(InfoHelper.GetInstalledLocation(), "Files.App", "Assets", "FilesOpenDialog")))
		{
			if (!SafetyExtensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), App.Logger))
			{
				// Error copying files
				goto DetectResult;
			}
		}

		try
		{
            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.OpenDialog32.dll")}"""))
            {
                await regProc.WaitForExitAsync();
            }
            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.OpenDialog64.dll")}"""))
            {
                await regProc.WaitForExitAsync();
            }
            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.OpenDialogARM64.dll")}"""))
            {
                await regProc.WaitForExitAsync();
            }
            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.SaveDialog32.dll")}"""))
            {
                await regProc.WaitForExitAsync();
            }
            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.SaveDialog64.dll")}"""))
            {
                await regProc.WaitForExitAsync();
            }
            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.SaveDialogARM64.dll")}"""))
            {
                await regProc.WaitForExitAsync();
            }
        }
        catch
		{
		}

	DetectResult:
		IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();
	}

	private async Task ImportSettingsAsync()
	{
		var filePicker = InitializeWithWindow(new FileOpenPicker());
		filePicker.FileTypeFilter.Add(".zip");

		var file = await filePicker.PickSingleFileAsync();
		if (file is not null)
		{
			try
			{
				var zipFolder = await ZipStorageFolder.FromStorageFileAsync(file);
				if (zipFolder is null)
                {
                    return;
                }

                // CHANGE: Use LocalSettingsExtensions instead of Package.Current.
                var localFolderPath = LocalSettingsExtensions.GetApplicationDataFolder("Files");
				var settingsFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(localFolderPath, Constants.LocalSettings.SettingsFolderName));

				// Import user settings
				var userSettingsFile = await zipFolder.GetFileAsync(Constants.LocalSettings.UserSettingsFileName);
				var importSettings = await userSettingsFile.ReadTextAsync();
				UserSettingsService.ImportSettings(importSettings);

                // Import file tags list and DB
                var fileTagsList = await zipFolder.GetFileAsync(Constants.LocalSettings.FileTagSettingsFileName);
                var importTags = await fileTagsList.ReadTextAsync();
                fileTagsSettingsService.ImportSettings(importTags);
                var fileTagsDB = await zipFolder.GetFileAsync(Constants.LocalSettings.FileTagSettingsDatabaseFileName);
                var importTagsDB = await fileTagsDB.ReadTextAsync();
                var tagDbInstance = FileTagsHelper.GetDbInstance();
                tagDbInstance.Import(importTagsDB);

                // Import layout preferences and DB
                var layoutPrefsDB = await zipFolder.GetFileAsync(Constants.LocalSettings.UserSettingsDatabaseFileName);
                var importPrefsDB = await layoutPrefsDB.ReadTextAsync();
                var layoutDbInstance = LayoutPreferencesManager.GetDatabaseManagerInstance();
                layoutDbInstance.Import(importPrefsDB);
            }
            catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Error importing settings");
				UIHelpers.CloseAllDialogs(FolderViewViewModel);
				await DialogDisplayHelper.ShowDialogAsync(FolderViewViewModel, "SettingsImportErrorTitle".GetLocalizedResource(), "SettingsImportErrorDescription".GetLocalizedResource());
			}
		}
	}

	private async Task ExportSettingsAsync()
	{
        var filePicker = InitializeWithWindow(new FileSavePicker());
        filePicker.FileTypeChoices.Add("Zip File", [".zip"]);
        filePicker.SuggestedFileName = $"Files_{AppLifecycleHelper.AppVersion}";

        var file = await filePicker.PickSaveFileAsync();
		if (file is not null)
		{
			try
			{
				await ZipStorageFolder.InitArchive(file, OutArchiveFormat.Zip);

				var zipFolder = (ZipStorageFolder)await ZipStorageFolder.FromStorageFileAsync(file);
				if (zipFolder is null)
                {
                    return;
                }

                // CHANGE: Use LocalSettingsExtensions instead of Package.Current.
                var localFolderPath = LocalSettingsExtensions.GetApplicationDataFolder("Files");

                // Export user settings
                var exportSettings = UTF8Encoding.UTF8.GetBytes((string)UserSettingsService.ExportSettings());
                await zipFolder.CreateFileAsync(new MemoryStream(exportSettings), Constants.LocalSettings.UserSettingsFileName, CreationCollisionOption.ReplaceExisting);

                // Export file tags list and DB
                var exportTags = UTF8Encoding.UTF8.GetBytes((string)fileTagsSettingsService.ExportSettings());
                await zipFolder.CreateFileAsync(new MemoryStream(exportTags), Constants.LocalSettings.FileTagSettingsFileName, CreationCollisionOption.ReplaceExisting);
                var tagDbInstance = FileTagsHelper.GetDbInstance();
                var exportTagsDB = UTF8Encoding.UTF8.GetBytes(tagDbInstance.Export());
                await zipFolder.CreateFileAsync(new MemoryStream(exportTagsDB), Constants.LocalSettings.FileTagSettingsDatabaseFileName, CreationCollisionOption.ReplaceExisting);

                // Export layout preferences DB
                var layoutDbInstance = LayoutPreferencesManager.GetDatabaseManagerInstance();
                var exportPrefsDB = UTF8Encoding.UTF8.GetBytes(layoutDbInstance.Export(FolderViewViewModel));
                await zipFolder.CreateFileAsync(new MemoryStream(exportPrefsDB), Constants.LocalSettings.UserSettingsDatabaseFileName, CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Error exporting settings");
			}
		}
	}

	private static bool DetectIsSetAsDefaultFileManager()
	{
		using var subkey = Registry.ClassesRoot.OpenSubKey(@"Folder\shell\open\command");
		var command = (string?)subkey?.GetValue(string.Empty);

		return !string.IsNullOrEmpty(command) && command.Contains("Files.App.Launcher.exe");
	}

    private static bool DetectIsSetAsOpenFileDialog()
    {
        using var subkeyOpen = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}");
        using var subkeySave = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{C0B4E2F3-BA21-4773-8DBA-335EC946EB8B}");

        var isSetAsOpenDialog = subkeyOpen?.GetValue(string.Empty) as string == "FilesOpenDialog class";
        var isSetAsSaveDialog = subkeySave?.GetValue(string.Empty) as string == "FilesSaveDialog class";

        return isSetAsOpenDialog || isSetAsSaveDialog;
    }

    private bool isSetAsDefaultFileManager;
	public bool IsSetAsDefaultFileManager
	{
		get => isSetAsDefaultFileManager;
		set => SetProperty(ref isSetAsDefaultFileManager, value);
	}

	private bool isSetAsOpenFileDialog;
	public bool IsSetAsOpenFileDialog
	{
		get => isSetAsOpenFileDialog;
		set => SetProperty(ref isSetAsOpenFileDialog, value);
	}

    public bool CanShowSetAsOpenFileDialog => AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev;

    private FileSavePicker InitializeWithWindow(FileSavePicker obj)
	{
		WinRT.Interop.InitializeWithWindow.Initialize(obj, FolderViewViewModel.WindowHandle);

		return obj;
	}

	private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
	{
		WinRT.Interop.InitializeWithWindow.Initialize(obj, FolderViewViewModel.WindowHandle);

		return obj;
	}

	private bool openOnWindowsStartup;
	public bool OpenOnWindowsStartup
	{
		get => openOnWindowsStartup;
		set => SetProperty(ref openOnWindowsStartup, value);
	}

	private bool canOpenOnWindowsStartup;
	public bool CanOpenOnWindowsStartup
	{
		get => canOpenOnWindowsStartup;
		set => SetProperty(ref canOpenOnWindowsStartup, value);
	}

	public bool LeaveAppRunning
	{
		get => UserSettingsService.GeneralSettingsService.LeaveAppRunning;
		set
		{
			if (value != UserSettingsService.GeneralSettingsService.LeaveAppRunning)
			{
				UserSettingsService.GeneralSettingsService.LeaveAppRunning = value;

				OnPropertyChanged();
			}
		}
	}

	public async Task OpenFilesOnWindowsStartupAsync()
	{
		var stateMode = await ReadState();

		var state = stateMode switch
		{
			StartupTaskState.Enabled => true,
			StartupTaskState.EnabledByPolicy => true,
			StartupTaskState.DisabledByPolicy => false,
			StartupTaskState.DisabledByUser => false,
			_ => false,
		};

		if (state != OpenOnWindowsStartup)
		{
			var startupTask = await StartupTask.GetAsync("3AA55462-A5FA-4933-88C4-712D0B6CDEBB");
			if (OpenOnWindowsStartup)
            {
                await startupTask.RequestEnableAsync();
            }
            else
            {
                startupTask.Disable();
            }

            await DetectOpenFilesAtStartupAsync();
		}
	}

	public async Task DetectOpenFilesAtStartupAsync()
	{
		var stateMode = await ReadState();

		switch (stateMode)
		{
			case StartupTaskState.Disabled:
				CanOpenOnWindowsStartup = true;
				OpenOnWindowsStartup = false;
				break;
			case StartupTaskState.Enabled:
				CanOpenOnWindowsStartup = true;
				OpenOnWindowsStartup = true;
				break;
			case StartupTaskState.DisabledByPolicy:
				CanOpenOnWindowsStartup = false;
				OpenOnWindowsStartup = false;
				break;
			case StartupTaskState.DisabledByUser:
				CanOpenOnWindowsStartup = false;
				OpenOnWindowsStartup = false;
				break;
			case StartupTaskState.EnabledByPolicy:
				CanOpenOnWindowsStartup = false;
				OpenOnWindowsStartup = true;
				break;
		}
	}

	public static async Task<StartupTaskState> ReadState()
	{
		var state = await StartupTask.GetAsync("3AA55462-A5FA-4933-88C4-712D0B6CDEBB");
		return state.State;
	}
}
