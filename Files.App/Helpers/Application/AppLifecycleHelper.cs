// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Notifications;
using Files.App.Storage.FtpStorage;
using Files.App.Storage.NativeStorage;
using Files.App.ViewModels.Settings;
using Files.App.Services.SizeProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using Windows.System;
using Windows.UI.Notifications;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Files.App.Helpers;

/// <summary>
/// Provides static helper to manage app lifecycle.
/// </summary>
public static class AppLifecycleHelper
{
    /// <summary>
    /// Gets the value that provides application environment or branch name.
    /// </summary>
    public static AppEnvironment AppEnvironment { get; } =
#if STORE
		AppEnvironment.Store;
#elif PREVIEW
		AppEnvironment.Preview;
#elif STABLE
		AppEnvironment.Stable;
#else
        AppEnvironment.Dev;
#endif

    /// <summary>
    /// Gets application package version.
    /// </summary>
    // CHANGE: Use InfoHelper instead of Package.Current.
    public static Version AppVersion { get; } =
        InfoHelper.GetVersion();

    /// <summary>
    /// Gets application icon path.
    /// </summary>
    // CHANGE: Use InfoHelper instead of Package.Current.
    public static string AppIconPath { get; } =
        Path.Combine(InfoHelper.GetInstalledLocation(), AppEnvironment switch
        {
            AppEnvironment.Dev => Constants.AssetPaths.DevLogo,
            AppEnvironment.Preview => Constants.AssetPaths.PreviewLogo,
            _ => Constants.AssetPaths.StableLogo
        });

    private static bool isInitialized = false;

    private static bool isCloudDrivesManagerInitialized = false;
    private static bool isWSLDistroManagerInitialized = false;
    private static bool isFileTagsManagerInitialized = false;

	/// <summary>
	/// Initializes the app components.
	/// </summary>
	public static async Task InitializeAppComponentsAsync(IFolderViewViewModel folderViewViewModel)
	{
        var userSettingsService = folderViewViewModel.GetRequiredService<IUserSettingsService>();
		var generalSettingsService = userSettingsService.GeneralSettingsService;

        // Start off a list of tasks we need to run before we can continue startup
        await Task.WhenAll(
			OptionalTaskAsync(CloudDrivesManager.UpdateDrivesAsync(), !isCloudDrivesManagerInitialized && generalSettingsService.ShowCloudDrivesSection),
            OptionalTaskAsync(App.LibraryManager.UpdateLibrariesAsync(), !isInitialized),
			OptionalTaskAsync(WSLDistroManager.UpdateDrivesAsync(), !isWSLDistroManagerInitialized && generalSettingsService.ShowWslSection),
            OptionalTaskAsync(App.FileTagsManager.UpdateFileTagsAsync(), !isFileTagsManagerInitialized && generalSettingsService.ShowFileTagsSection),
            OptionalTaskAsync(App.QuickAccessManager.InitializeAsync(), !isInitialized)
        );

        isCloudDrivesManagerInitialized = isCloudDrivesManagerInitialized is true || generalSettingsService.ShowCloudDrivesSection;
        isWSLDistroManagerInitialized = isWSLDistroManagerInitialized is true || generalSettingsService.ShowWslSection;
        isFileTagsManagerInitialized = isFileTagsManagerInitialized is true || generalSettingsService.ShowFileTagsSection;

        if (isInitialized)
        {
            return;
        }

        var addItemService = DependencyExtensions.GetRequiredService<IAddItemService>();
        var jumpListService = DependencyExtensions.GetRequiredService<IWindowsJumpListService>();
        await Task.WhenAll(
            jumpListService.InitializeAsync(),
            addItemService.InitializeAsync(),
			ContextMenu.WarmUpQueryContextMenuAsync()
		);

		FileTagsHelper.UpdateTagsDb();

        // CHANGE: Remove update check.
		/*await CheckAppUpdate(folderViewViewModel);*/

		static Task OptionalTaskAsync(Task task, bool condition)
		{
			if (condition)
            {
                return task;
            }

            return Task.CompletedTask;
		}

        isInitialized = true;
	}

	/// <summary>
	/// Checks application updates and download if available.
	/// </summary>
	public static async Task CheckAppUpdate(IFolderViewViewModel folderViewViewModel)
	{
        var updateService = DependencyExtensions.GetRequiredService<IUpdateService>();

		await updateService.CheckForUpdatesAsync(folderViewViewModel);
		await updateService.DownloadMandatoryUpdatesAsync(folderViewViewModel);
		await updateService.CheckAndUpdateFilesLauncherAsync();
		await updateService.CheckLatestReleaseNotesAsync();
	}

	/// <summary>
	/// Configures AppCenter service, such as Analytics and Crash Report.
	/// </summary>
	public static void ConfigureAppCenter()
	{
		try
		{
			if (!Microsoft.AppCenter.AppCenter.Configured)
			{
				Microsoft.AppCenter.AppCenter.Start(
					Constants.AutomatedWorkflowInjectionKeys.AppCenterSecret,
					typeof(Microsoft.AppCenter.Analytics.Analytics),
					typeof(Microsoft.AppCenter.Crashes.Crashes));
			}
		}
		catch (Exception ex)
		{
			App.Logger?.LogWarning(ex, "Failed to start AppCenter service.");
		}
	}

    /// <summary>
	/// Configures DI (dependency injection) container.
	/// </summary>
    public static IHostBuilder ConfigureHost(this IHostBuilder host)
    {
        return host
            .UseEnvironment(AppLifecycleHelper.AppEnvironment.ToString())
            .ConfigureLogging(builder => builder
                    .AddProvider(new FileLoggerProvider(Path.Combine(LocalSettingsExtensions.GetApplicationDataFolder("Files"), "debug.log")))
                    .SetMinimumLevel(LogLevel.Information))
            .ConfigureServices(services => services
                // Settings services
                .AddTransient<IUserSettingsService, UserSettingsService>()
                .AddTransient<IAppearanceSettingsService, AppearanceSettingsService>()
                .AddTransient<IGeneralSettingsService, GeneralSettingsService>()
                .AddTransient<IFoldersSettingsService, FoldersSettingsService>()
                .AddTransient<IApplicationSettingsService, ApplicationSettingsService>()
                .AddTransient<IInfoPaneSettingsService, InfoPaneSettingsService>()
                .AddTransient<ILayoutSettingsService, LayoutSettingsService>()
                .AddTransient<IAppSettingsService, AppSettingsService>()
                .AddTransient<IActionsSettingsService, ActionsSettingsService>()
                .AddSingleton<IFileTagsSettingsService, FileTagsSettingsService>()
                // Contexts
                .AddTransient<IPageContext, PageContext>()
                .AddTransient<IContentPageContext, ContentPageContext>()
                .AddTransient<IDisplayPageContext, DisplayPageContext>()
                .AddSingleton<IHomePageContext, HomePageContext>()
                .AddTransient<IWindowContext, WindowContext>()
                .AddTransient<IMultitaskingContext, MultitaskingContext>()
                .AddSingleton<ITagsContext, TagsContext>()
                // Services
                .AddSingleton<IAppThemeModeService, AppThemeModeService>()
                .AddTransient<IDialogService, DialogService>()
                .AddSingleton<IImageService, ImagingService>()
                .AddSingleton<IThreadingService, ThreadingService>()
                .AddSingleton<ILocalizationService, LocalizationService>()
                .AddSingleton<ICloudDetector, CloudDetector>()
                .AddSingleton<IFileTagsService, FileTagsService>()
                .AddTransient<ICommandManager, CommandManager>()
                .AddTransient<IModifiableCommandManager, ModifiableCommandManager>()
                .AddSingleton<IStorageService, NativeStorageService>()
                .AddSingleton<IFtpStorageService, FtpStorageService>()
                .AddSingleton<IAddItemService, AddItemService>()
#if STABLE || PREVIEW
			    .AddSingleton<IUpdateService, SideloadUpdateService>()
#else
                .AddSingleton<IUpdateService, UpdateService>()
#endif
                .AddSingleton<IPreviewPopupService, PreviewPopupService>()
                .AddSingleton<IDateTimeFormatterFactory, DateTimeFormatterFactory>()
                .AddTransient<IDateTimeFormatter, UserDateTimeFormatter>()
                .AddSingleton<IVolumeInfoFactory, VolumeInfoFactory>()
                .AddTransient<ISizeProvider, UserSizeProvider>()
                .AddSingleton<IQuickAccessService, QuickAccessService>()
                .AddSingleton<IResourcesService, ResourcesService>()
                .AddSingleton<IWindowsJumpListService, WindowsJumpListService>()
                .AddSingleton<IRemovableDrivesService, RemovableDrivesService>()
                .AddSingleton<INetworkDrivesService, NetworkDrivesService>()
                .AddSingleton<IStartMenuService, StartMenuService>()
                .AddSingleton<IStorageCacheService, StorageCacheService>()
                .AddSingleton<IWindowsCompatibilityService, WindowsCompatibilityService>()
                // ViewModels
                .AddTransient<MainPageViewModel>()
                .AddTransient<InfoPaneViewModel>()
                .AddTransient<SidebarViewModel>()
                /*.AddSingleton<SettingsViewModel>() // deprecated service*/
                .AddSingleton<DrivesViewModel>()
                .AddTransient<StatusCenterViewModel>()
                .AddSingleton<AppearanceViewModel>()
                .AddTransient<HomeViewModel>()
                .AddTransient<QuickAccessWidgetViewModel>()
                .AddTransient<DrivesWidgetViewModel>()
                .AddTransient<FileTagsWidgetViewModel>()
                .AddTransient<RecentFilesWidgetViewModel>()
                // Utilities
                .AddSingleton<QuickAccessManager>()
                .AddSingleton<StorageHistoryWrapper>()
                .AddSingleton<FileTagsManager>()
                .AddSingleton<RecentItems>()
                .AddSingleton<LibraryManager>()
                .AddSingleton<AppModel>()
            );
    }

    /// <summary>
    /// Saves saves all opened tabs to the app cache.
    /// </summary>
    public static void SaveSessionTabs(IFolderViewViewModel? folderViewViewModel = null)
	{
        if (folderViewViewModel is null)
        {
            foreach (var folderView in App.FolderViewViewModels)
            {
                SaveSessionTabs(folderView);
            }
            return;
        }

		var userSettingsService = folderViewViewModel.GetRequiredService<IUserSettingsService>();

		userSettingsService.GeneralSettingsService.LastSessionTabList = MainPageViewModel.AppInstancesManager.Get(folderViewViewModel).DefaultIfEmpty().Select(tab =>
		{
			if (tab is not null && tab.NavigationParameter is not null)
			{
				return tab.NavigationParameter.Serialize();
			}
			else
			{
				var defaultArg = new CustomTabViewItemParameter()
				{
                    FolderViewViewModel = folderViewViewModel,
					InitialPageType = typeof(PaneHolderPage),
					NavigationParameter = "Home"
				};

				return defaultArg.Serialize();
			}
		})
		.ToList();
	}

	/// <summary>
	/// Shows exception on the Debug Output and sends Toast Notification to the Windows Notification Center.
	/// </summary>
	public static void HandleAppUnhandledException(Exception? ex, bool showToastNotification)
	{
		StringBuilder formattedException = new()
		{
			Capacity = 200
		};

		formattedException.AppendLine("--------- UNHANDLED EXCEPTION ---------");

		if (ex is not null)
		{
			formattedException.AppendLine($">>>> HRESULT: {ex.HResult}");

			if (ex.Message is not null)
			{
				formattedException.AppendLine("--- MESSAGE ---");
				formattedException.AppendLine(ex.Message);
			}
			if (ex.StackTrace is not null)
			{
				formattedException.AppendLine("--- STACKTRACE ---");
				formattedException.AppendLine(ex.StackTrace);
			}
			if (ex.Source is not null)
			{
				formattedException.AppendLine("--- SOURCE ---");
				formattedException.AppendLine(ex.Source);
			}
			if (ex.InnerException is not null)
			{
				formattedException.AppendLine("--- INNER ---");
				formattedException.AppendLine(ex.InnerException.ToString());
			}
		}
		else
		{
			formattedException.AppendLine("Exception data is not available.");
		}

		formattedException.AppendLine("---------------------------------------");

		Debug.WriteLine(formattedException.ToString());

        // Please check "Output Window" for exception details (View -> Output Window) (CTRL + ALT + O)
        Debugger.Break();

        // Save the current tab list in case it was overwriten by another instance
        SaveSessionTabs();
        App.Logger?.LogError(ex, ex?.Message ?? "An unhandled error occurred.");

        if (!showToastNotification)
        {
            return;
        }

        SafetyExtensions.IgnoreExceptions(() =>
        {
            var toastContent = new ToastContent()
		    {
			    Visual = new()
			    {
				    BindingGeneric = new ToastBindingGeneric()
				    {
					    Children =
					    {
						    new AdaptiveText()
						    {
							    Text = "ExceptionNotificationHeader".GetLocalizedResource()
						    },
						    new AdaptiveText()
						    {
							    Text = "ExceptionNotificationBody".GetLocalizedResource()
						    }
					    },
					    AppLogoOverride = new()
					    {
						    Source = "ms-appx:///Files.App/Assets/error.png"
					    }
				    }
			    },
			    Actions = new ToastActionsCustom()
			    {
				    Buttons =
				    {
                        new ToastButton("ExceptionNotificationReportButton".GetLocalizedResource(), Constants.ExternalUrl.BugReportUrl)
                        {
                            ActivationType = ToastActivationType.Protocol
					    }
				    }
			    },
			    ActivationType = ToastActivationType.Protocol
		    };

		    // Create the toast notification
		    var toastNotification = new ToastNotification(toastContent.GetXml());

		    // And send the notification
		    ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
        });

        // Restart the app
        var userSettingsService = DependencyExtensions.GetRequiredService<IUserSettingsService>();
		var lastSessionTabList = userSettingsService.GeneralSettingsService.LastSessionTabList;

		if (userSettingsService.GeneralSettingsService.LastCrashedTabList?.SequenceEqual(lastSessionTabList) ?? false)
		{
			// Avoid infinite restart loop
			userSettingsService.GeneralSettingsService.LastSessionTabList = null!;
		}
		else
		{
			userSettingsService.AppSettingsService.RestoreTabsOnStartup = true;
			userSettingsService.GeneralSettingsService.LastCrashedTabList = lastSessionTabList;

			// Try to re-launch and start over
			ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				await Launcher.LaunchUriAsync(new Uri("files-uwp:"));
			})
			.Wait(100);
		}
		Process.GetCurrentProcess().Kill();
	}

    /// <summary>
    ///	Checks if the taskbar is set to auto-hide.
    /// </summary>
    public static bool IsAutoHideTaskbarEnabled()
    {
        const string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3";
        const string valueName = "Settings";

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryKey);

        // The least significant bit of the 9th byte controls the auto-hide setting																		
        return key?.GetValue(valueName) is byte[] value && ((value[8] & 0x01) == 1);
    }
}
