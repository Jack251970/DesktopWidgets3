// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Services.Settings;
using LibGit2Sharp;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Windows.Input;
using Windows.System;

namespace Files.App.ViewModels;

/// <summary>
/// Represents ViewModel of <see cref="MainPage"/>.
/// </summary>
public class MainPageViewModel : ObservableObject
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    // Dependency injections

    /*private IAppearanceSettingsService AppearanceSettingsService { get; set; } = null!;*/
	private NetworkDrivesViewModel NetworkDrivesViewModel { get; } = DependencyExtensions.GetService<NetworkDrivesViewModel>();
    private IUserSettingsService UserSettingsService { get; set; } = null!;
	/*private IResourcesService ResourcesService { get; } = DependencyExtensions.GetService<IResourcesService>();*/
	private DrivesViewModel DrivesViewModel { get; } = DependencyExtensions.GetService<DrivesViewModel>();

    // Properties

    /*public static ObservableCollection<TabBarItem> AppInstances { get; private set; } = new();

    public List<ITabBar> MultitaskingControls { get; } = new();

	public ITabBar? MultitaskingControl { get; set; }*/

    private TabBarItem? selectedTabItem;
	public TabBarItem? SelectedTabItem
	{
		get => selectedTabItem;
		set => SetProperty(ref selectedTabItem, value);
	}

	// Commands

	/*public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand { get; }
	public ICommand OpenNewWindowAcceleratorCommand { get; }*/

	// Constructor

	public MainPageViewModel()
	{
		/*NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(ExecuteNavigateToNumberedTabKeyboardAcceleratorCommand);
		OpenNewWindowAcceleratorCommand = new AsyncRelayCommand<KeyboardAcceleratorInvokedEventArgs>(ExecuteOpenNewWindowAcceleratorCommand);*/
	}

	// Methods

	public async Task OnNavigatedToAsync(NavigationEventArgs e)
	{
		if (e.NavigationMode == NavigationMode.Back)
        {
            return;
        }

        // CHANGE: Initialize folder view view model and related services.
        if (e.Parameter is IFolderViewViewModel folderViewViewModel)
        {
            FolderViewViewModel = folderViewViewModel;

            /*AppearanceSettingsService = folderViewViewModel.GetRequiredService<IAppearanceSettingsService>();*/
            UserSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
        }

        // Initialize the static theme helper to capture a reference to this window
        // to handle theme changes without restarting the app
        var isInitialized = ThemeHelper.Initialize();

        // CHANGE: Disable event argument handling.
        /*var parameter = e.Parameter;
		var ignoreStartupSettings = false;
        if (parameter is MainPageNavigationArguments mainPageNavigationArguments)
		{
			parameter = mainPageNavigationArguments.Parameter;
			ignoreStartupSettings = mainPageNavigationArguments.IgnoreStartupSettings;
		}*/

        /*if (parameter is null || (parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
		{
			try
			{
				// add last session tabs to closed tabs stack if those tabs are not about to be opened
				if (!UserSettingsService.AppSettingsService.RestoreTabsOnStartup && !UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp && UserSettingsService.GeneralSettingsService.LastSessionTabList != null)
				{
					var items = new CustomTabViewItemParameter[UserSettingsService.GeneralSettingsService.LastSessionTabList.Count];
					for (var i = 0; i < items.Length; i++)
                    {
                        items[i] = CustomTabViewItemParameter.Deserialize(UserSettingsService.GeneralSettingsService.LastSessionTabList[i]);
                    }

                    BaseTabBar.PushRecentTab(items);
				}

				if (UserSettingsService.AppSettingsService.RestoreTabsOnStartup)
				{
					UserSettingsService.AppSettingsService.RestoreTabsOnStartup = false;
					if (UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						foreach (var tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
							await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
						}

						if (!UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
                        {
                            UserSettingsService.GeneralSettingsService.LastSessionTabList = null!;
                        }
                    }
				}
				else if (UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
					UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
				{
					foreach (var path in UserSettingsService.GeneralSettingsService.TabsOnStartupList)
                    {
                        await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), path);
                    }
                }
				else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
					UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
				{
					foreach (var tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
					{
						var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
						await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
					}

					var defaultArg = new CustomTabViewItemParameter() { InitialPageType = typeof(PaneHolderPage), NavigationParameter = "Home" };

					UserSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
				}
				else
				{
					await NavigationHelpers.AddNewTabAsync();
				}
			}
			catch
			{
				await NavigationHelpers.AddNewTabAsync();
			}
		}
		else
		{
			if (!ignoreStartupSettings)
			{
				try
				{
					if (UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
							UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
					{
						foreach (var path in UserSettingsService.GeneralSettingsService.TabsOnStartupList)
                        {
                            await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), path);
                        }
                    }
					else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						foreach (var tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = CustomTabViewItemParameter.Deserialize(tabArgsString);
							await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
						}

						var defaultArg = new CustomTabViewItemParameter() { InitialPageType = typeof(PaneHolderPage), NavigationParameter = "Home" };

						UserSettingsService.GeneralSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
					}
				}
				catch { }
			}

			if (parameter is string navArgs)
            {
                await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
            }
            else if (parameter is PaneNavigationArguments paneArgs)
            {
                await NavigationHelpers.AddNewTabByParamAsync(typeof(PaneHolderPage), paneArgs);
            }
            else if (parameter is CustomTabViewItemParameter tabArgs)
            {
                await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
            }
        }*/

        try
        {
            if (UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
            UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
            {
                foreach (var path in UserSettingsService.GeneralSettingsService.TabsOnStartupList)
                {
                    await NavigationHelpers.AddNewTabByPathAsync(FolderViewViewModel, typeof(PaneHolderPage), path);
                }
            }
            else
            {
                await NavigationHelpers.AddNewTabAsync(FolderViewViewModel);
            }
        }
        catch
        {
            await NavigationHelpers.AddNewTabAsync(FolderViewViewModel);
        }
        

        if (isInitialized)
		{
			// Load the app theme resources
			/*ResourcesService.LoadAppResources(AppearanceSettingsService);*/

			await Task.WhenAll(
				DrivesViewModel.UpdateDrivesAsync(),
				NetworkDrivesViewModel.UpdateDrivesAsync());
		}
	}

	// Command methods

	/*private void ExecuteNavigateToNumberedTabKeyboardAcceleratorCommand(KeyboardAcceleratorInvokedEventArgs? e)
	{
		var indexToSelect = e!.KeyboardAccelerator.Key switch
		{
			VirtualKey.Number1 => 0,
			VirtualKey.Number2 => 1,
			VirtualKey.Number3 => 2,
			VirtualKey.Number4 => 3,
			VirtualKey.Number5 => 4,
			VirtualKey.Number6 => 5,
			VirtualKey.Number7 => 6,
			VirtualKey.Number8 => 7,
			VirtualKey.Number9 => AppInstances.Count - 1,
			_ => AppInstances.Count - 1,
		};

		// Only select the tab if it is in the list
		if (indexToSelect < AppInstances.Count)
        {
            DependencyExtensions.GetService<AppModel>().TabStripSelectedIndex = indexToSelect;
        }

        e.Handled = true;
	}

	private async Task ExecuteOpenNewWindowAcceleratorCommand(KeyboardAcceleratorInvokedEventArgs? e)
	{
		await Launcher.LaunchUriAsync(new Uri("files-uwp:"));

		e!.Handled = true;
	}*/
}
