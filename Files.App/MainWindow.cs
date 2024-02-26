// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Core.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace Files.App;

#pragma warning disable CS0618 // Type or member is obsolete

public sealed partial class MainWindow
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IApplicationService ApplicationService;

    /*private MainPageViewModel mainPageViewModel;

    private static MainWindow? _Instance;
	public static MainWindow Instance => _Instance ??= new();*/

    public IntPtr WindowHandle { get; }

    public MainWindow(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

        ApplicationService = new ApplicationService();

        WindowHandle = folderViewViewModel.WindowHandle;

        /*InitializeComponent();*/

        EnsureEarlyWindow(folderViewViewModel);
    }

    private void EnsureEarlyWindow(IFolderViewViewModel folderViewViewModel)
	{
		// Set PersistenceId
		folderViewViewModel.MainWindow.PersistenceId = "FilesMainWindow";

        // Set minimum sizes
        folderViewViewModel.MainWindow.MinHeight = 416;
        folderViewViewModel.MainWindow.MinWidth = 516;

        folderViewViewModel.MainWindow.AppWindow.Title = "Files";
        folderViewViewModel.MainWindow.AppWindow.SetIcon(Path.Combine(InfoHelper.GetInstalledLocation(), ApplicationService.AppIcoPath));
        
        // CHANGE: Don't set title bar.
        /*folderViewViewModel.MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        folderViewViewModel.MainWindow.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        folderViewViewModel.MainWindow.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;*/

		// Workaround for full screen window messing up the taskbar
		// https://github.com/microsoft/microsoft-ui-xaml/issues/8431
		InteropHelpers.SetPropW(WindowHandle, "NonRudeHWND", new IntPtr(1));
	}

    public void ShowSplashScreen()
	{
		var rootFrame = EnsureWindowIsInitialized();

		rootFrame.Navigate(typeof(SplashScreenPage), FolderViewViewModel);
	}

	public async Task InitializeApplicationAsync(object activatedEventArgs)
	{
        // CHANGE: Don't set system backdrop.
		/*// Set system backdrop
		FolderViewViewModel.MainWindow.SystemBackdrop = new AppSystemBackdrop(FolderViewViewModel);*/

		var rootFrame = EnsureWindowIsInitialized();

        switch (activatedEventArgs)
		{
            // CHANGE: Use string & startup tabs function to navigate to specific path.
            case string folderPath:
                var _userSettingsService = FolderViewViewModel.GetService<IUserSettingsService>();
                _userSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup = true;
                _userSettingsService.GeneralSettingsService.TabsOnStartupList = new List<string>() { folderPath };
                rootFrame.Navigate(typeof(MainPage), FolderViewViewModel, new SuppressNavigationTransitionInfo());
                break;

			case ILaunchActivatedEventArgs launchArgs:
				if (launchArgs.Arguments is not null &&
					(CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files.exe", StringComparison.OrdinalIgnoreCase)
					|| CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files", StringComparison.OrdinalIgnoreCase)))
				{
					// WINUI3: When launching from commandline the argument is not ICommandLineActivatedEventArgs (#10370)
					var ppm = CommandLineParser.ParseUntrustedCommands(launchArgs.Arguments);
					if (ppm.IsEmpty())
                    {
                        rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                    }
                    else
                    {
                        await InitializeFromCmdLineArgsAsync(rootFrame, ppm);
                    }
                }
				else if (rootFrame.Content is null || rootFrame.Content is SplashScreenPage || !MainPageViewModel.AppInstances[FolderViewViewModel].Any())
				{
					// When the navigation stack isn't restored navigate to the first page,
					// configuring the new page by passing required information as a navigation parameter
					rootFrame.Navigate(typeof(MainPage), launchArgs.Arguments, new SuppressNavigationTransitionInfo());
				}
				else if (!(string.IsNullOrEmpty(launchArgs.Arguments) && MainPageViewModel.AppInstances[FolderViewViewModel].Count > 0))
				{
					InteropHelpers.SwitchToThisWindow(FolderViewViewModel.WindowHandle, true);
					await NavigationHelpers.AddNewTabByPathAsync(FolderViewViewModel, typeof(PaneHolderPage), launchArgs.Arguments);
				}
				else
				{
					rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
				}
				break;

			case IProtocolActivatedEventArgs eventArgs:
				if (eventArgs.Uri.AbsoluteUri == "files-uwp:")
				{
					rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
				}
				else
				{
					var parsedArgs = eventArgs.Uri.Query.TrimStart('?').Split('=');
					var unescapedValue = Uri.UnescapeDataString(parsedArgs[1]);
					var folder = (StorageFolder)await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(unescapedValue).AsTask());
					if (folder is not null && !string.IsNullOrEmpty(folder.Path))
					{
						// Convert short name to long name (#6190)
						unescapedValue = folder.Path;
					}
					switch (parsedArgs[0])
					{
						case "tab":
							rootFrame.Navigate(typeof(MainPage),
								new MainPageNavigationArguments() { Parameter = CustomTabViewItemParameter.Deserialize(FolderViewViewModel, unescapedValue), IgnoreStartupSettings = true },
								new SuppressNavigationTransitionInfo());
							break;

						case "folder":
							rootFrame.Navigate(typeof(MainPage),
								new MainPageNavigationArguments() { Parameter = unescapedValue, IgnoreStartupSettings = true },
								new SuppressNavigationTransitionInfo());
							break;

						case "cmd":
							var ppm = CommandLineParser.ParseUntrustedCommands(unescapedValue);
							if (ppm.IsEmpty())
                            {
                                rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                            }
                            else
                            {
                                await InitializeFromCmdLineArgsAsync(rootFrame, ppm);
                            }

                            break;
						default:
							rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
							break;
					}
				}
				break;

			case ICommandLineActivatedEventArgs cmdLineArgs:
				var operation = cmdLineArgs.Operation;
				var cmdLineString = operation.Arguments;
				var activationPath = operation.CurrentDirectoryPath;

				var parsedCommands = CommandLineParser.ParseUntrustedCommands(cmdLineString);
				if (parsedCommands is not null && parsedCommands.Count > 0)
				{
					await InitializeFromCmdLineArgsAsync(rootFrame, parsedCommands, activationPath);
				}
				else
				{
					rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
				}
				break;

			case IFileActivatedEventArgs fileArgs:
				var index = 0;
				if (rootFrame.Content is null || rootFrame.Content is SplashScreenPage || !MainPageViewModel.AppInstances[FolderViewViewModel].Any())
				{
					// When the navigation stack isn't restored navigate to the first page,
					// configuring the new page by passing required information as a navigation parameter
					rootFrame.Navigate(typeof(MainPage), fileArgs.Files[0].Path, new SuppressNavigationTransitionInfo());
					index = 1;
				}
				else
                {
                    InteropHelpers.SwitchToThisWindow(FolderViewViewModel.WindowHandle, true);
                }

                for (; index < fileArgs.Files.Count; index++)
				{
					await NavigationHelpers.AddNewTabByPathAsync(FolderViewViewModel, typeof(PaneHolderPage), fileArgs.Files[index].Path);
				}
				break;

			case IStartupTaskActivatedEventArgs startupArgs:
				// Just launch the app with no arguments
				rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
				break;

			default:
				// Just launch the app with no arguments
				rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
				break;
		}

		if (!FolderViewViewModel.MainWindow.AppWindow.IsVisible)
		{
            // When resuming the cached instance
            FolderViewViewModel.MainWindow.AppWindow.Show();
            FolderViewViewModel.MainWindow.Activate();
		}
	}

	public Frame EnsureWindowIsInitialized()
	{
		// NOTE:
		//  Do not repeat app initialization when the Window already has content,
		//  just ensure that the window is active
        // CHANGE: Use folder view page to replace main window.
		if (FolderViewViewModel.Page.Content is not Frame rootFrame)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			rootFrame = new() { CacheSize = 1 };
			rootFrame.NavigationFailed += OnNavigationFailed;

            // Place the frame in the current Window
            FolderViewViewModel.Page.Content = rootFrame;
		}

		return rootFrame;
	}

	/// <summary>
	/// Invoked when Navigation to a certain page fails
	/// </summary>
	/// <param name="sender">The Frame which failed navigation</param>
	/// <param name="e">Details about the navigation failure</param>
	private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		=> throw new Exception("Failed to load Page " + e.SourcePageType.FullName);

    private async Task InitializeFromCmdLineArgsAsync(Frame rootFrame, ParsedCommands parsedCommands, string activationPath = "")
	{
		async Task PerformNavigationAsync(string payload, string selectItem = null!)
		{
			if (!string.IsNullOrEmpty(payload))
			{
				payload = Constants.UserEnvironmentPaths.ShellPlaces.Get(payload.ToUpperInvariant(), payload)!;
				var folder = (StorageFolder)await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(payload).AsTask());
				if (folder is not null && !string.IsNullOrEmpty(folder.Path))
                {
                    payload = folder.Path; // Convert short name to long name (#6190)
                }
            }

			var generalSettingsService = FolderViewViewModel.GetService<IGeneralSettingsService>();
			var paneNavigationArgs = new PaneNavigationArguments
			{
                FolderViewViewModel = FolderViewViewModel,
				LeftPaneNavPathParam = payload,
				LeftPaneSelectItemParam = selectItem,
				RightPaneNavPathParam = FolderViewViewModel.MainWindow.Bounds.Width > PaneHolderPage.DualPaneWidthThreshold && (generalSettingsService?.AlwaysOpenDualPaneInNewTab ?? false) ? "Home" : null,
			};

            if (rootFrame.Content is MainPage && MainPageViewModel.AppInstances[FolderViewViewModel].Any())
			{
				InteropHelpers.SwitchToThisWindow(FolderViewViewModel.WindowHandle, true);
				await NavigationHelpers.AddNewTabByParamAsync(FolderViewViewModel, typeof(PaneHolderPage), paneNavigationArgs);
			}
			else
            {
                rootFrame.Navigate(typeof(MainPage), paneNavigationArgs, new SuppressNavigationTransitionInfo());
            }
        }
		foreach (var command in parsedCommands)
		{
			switch (command.Type)
			{
				case ParsedCommandType.OpenDirectory:
				case ParsedCommandType.OpenPath:
				case ParsedCommandType.ExplorerShellCommand:
					var selectItemCommand = parsedCommands.FirstOrDefault(x => x.Type == ParsedCommandType.SelectItem);
					await PerformNavigationAsync(command.Payload, selectItemCommand?.Payload!);
					break;

				case ParsedCommandType.SelectItem:
					if (Path.IsPathRooted(command.Payload))
                    {
                        await PerformNavigationAsync(Path.GetDirectoryName(command.Payload)!, Path.GetFileName(command.Payload));
                    }

                    break;

				case ParsedCommandType.TagFiles:
					var tagService = DependencyExtensions.GetService<IFileTagsSettingsService>();
					var tag = tagService.GetTagsByName(command.Payload).FirstOrDefault();
					foreach (var file in command.Args.Skip(1))
					{
						var fileFRN = await FilesystemTasks.Wrap(() => StorageHelpers.ToStorageItem<IStorageItem>(file))
							.OnSuccess(FileTagsHelper.GetFileFRN);
						if (fileFRN is not null)
						{
							var tagUid = tag is not null ? new[] { tag.Uid } : null;
							var dbInstance = FileTagsHelper.GetDbInstance();
							dbInstance.SetTags(file, fileFRN, tagUid);
							FileTagsHelper.WriteFileTag(file, tagUid!);
						}
					}
					break;

				case ParsedCommandType.Unknown:
					if (command.Payload.Equals("."))
					{
						await PerformNavigationAsync(activationPath);
					}
					else
					{
						if (!string.IsNullOrEmpty(command.Payload))
						{
							var target = Path.GetFullPath(Path.Combine(activationPath, command.Payload));
							await PerformNavigationAsync(target);
						}
						else
						{
							await PerformNavigationAsync(null!);
						}
					}
					break;

				case ParsedCommandType.OutputPath:
					App.OutputPath = command.Payload;
					break;
			}
		}
	}
}
