// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System.Windows.Input;
using Windows.System;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.ViewModels;

/// <summary>
/// Represents ViewModel of <see cref="MainPage"/>.
/// </summary>
public sealed class MainPageViewModel : ObservableObject
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    // Dependency injections

    private IAppearanceSettingsService AppearanceSettingsService { get; set; } = null!;
    private INetworkService NetworkService { get; } = DependencyExtensions.GetRequiredService<INetworkService>();
    private IUserSettingsService UserSettingsService { get; set; } = null!;
	/*private IResourcesService ResourcesService { get; } = DependencyExtensions.GetRequiredService<IResourcesService>();*/
	private DrivesViewModel DrivesViewModel { get; } = DependencyExtensions.GetRequiredService<DrivesViewModel>();

    // Properties

    // CHANGE: Use dictionary to support multiple folder view view models.
    /*public static ObservableCollection<TabBarItem> AppInstances { get; private set; } = [];*/
    private static readonly Dictionary<IFolderViewViewModel, ObservableCollection<TabBarItem>> MultiAppInstances = [];
    public static readonly DictionaryManager<ObservableCollection<TabBarItem>> AppInstancesManager = new(MultiAppInstances, () => []);

    public List<ITabBar> MultitaskingControls { get; } = [];

	public ITabBar? MultitaskingControl { get; set; }

    private TabBarItem? selectedTabItem;
	public TabBarItem? SelectedTabItem
	{
		get => selectedTabItem;
		set => SetProperty(ref selectedTabItem, value);
	}

    private bool shouldViewControlBeDisplayed;
    public bool ShouldViewControlBeDisplayed
    {
        get => shouldViewControlBeDisplayed;
        set => SetProperty(ref shouldViewControlBeDisplayed, value);
    }

    private bool shouldPreviewPaneBeActive;
    public bool ShouldPreviewPaneBeActive
    {
        get => shouldPreviewPaneBeActive;
        set => SetProperty(ref shouldPreviewPaneBeActive, value);
    }

    private bool shouldPreviewPaneBeDisplayed;
    public bool ShouldPreviewPaneBeDisplayed
    {
        get => shouldPreviewPaneBeDisplayed;
        set => SetProperty(ref shouldPreviewPaneBeDisplayed, value);
    }

    public Stretch AppThemeBackgroundImageFit
        => AppearanceSettingsService.AppThemeBackgroundImageFit;

    public float AppThemeBackgroundImageOpacity
        => AppearanceSettingsService.AppThemeBackgroundImageOpacity;

    public ImageSource? AppThemeBackgroundImageSource =>
        string.IsNullOrEmpty(AppearanceSettingsService.AppThemeBackgroundImageSource)
            ? null
            : new BitmapImage(new Uri(AppearanceSettingsService.AppThemeBackgroundImageSource, UriKind.RelativeOrAbsolute));

    public VerticalAlignment AppThemeBackgroundImageVerticalAlignment
        => AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment;

    public HorizontalAlignment AppThemeBackgroundImageHorizontalAlignment
        => AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment;

    public bool ShowToolbar
        => AppearanceSettingsService.ShowToolbar;

    // Commands

    public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand { get; }

	// Constructor

	public MainPageViewModel()
	{
        NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(ExecuteNavigateToNumberedTabKeyboardAcceleratorCommand);

        /*AppearanceSettingsService.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageSource):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageSource));
                    break;
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageOpacity):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageOpacity));
                    break;
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageFit):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageFit));
                    break;
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageVerticalAlignment));
                    break;
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageHorizontalAlignment));
                    break;
                case nameof(AppearanceSettingsService.ShowToolbar):
					OnPropertyChanged(nameof(ShowToolbar));
					break;
            }
        };*/
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

            AppearanceSettingsService = folderViewViewModel.GetRequiredService<IAppearanceSettingsService>();
            UserSettingsService = folderViewViewModel.GetRequiredService<IUserSettingsService>();
        }

        AppearanceSettingsService.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageSource):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageSource));
                    break;
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageOpacity):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageOpacity));
                    break;
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageFit):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageFit));
                    break;
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageVerticalAlignment));
                    break;
                case nameof(AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment):
                    OnPropertyChanged(nameof(AppThemeBackgroundImageHorizontalAlignment));
                    break;
                case nameof(AppearanceSettingsService.ShowToolbar):
                    OnPropertyChanged(nameof(ShowToolbar));
                    break;
            }
        };

        var parameter = e.Parameter;
		var ignoreStartupSettings = false;
        if (parameter is MainPageNavigationArguments mainPageNavigationArguments)
		{
			parameter = mainPageNavigationArguments.Parameter;
			ignoreStartupSettings = mainPageNavigationArguments.IgnoreStartupSettings;
		}

        if (parameter is null || (parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
		{
			try
			{
				// add last session tabs to closed tabs stack if those tabs are not about to be opened
				if (!UserSettingsService.AppSettingsService.RestoreTabsOnStartup && !UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp && UserSettingsService.GeneralSettingsService.LastSessionTabList != null)
				{
                    var items = new TabBarItemParameter[UserSettingsService.GeneralSettingsService.LastSessionTabList.Count];
                    for (var i = 0; i < items.Length; i++)
                    {
                        items[i] = TabBarItemParameter.Deserialize(FolderViewViewModel, UserSettingsService.GeneralSettingsService.LastSessionTabList[i]);
                    }

                    // CHANGE: Non-static function instead of static one.
                    MultitaskingControl!.PushRecentTab(items);
				}

				if (UserSettingsService.AppSettingsService.RestoreTabsOnStartup)
				{
					UserSettingsService.AppSettingsService.RestoreTabsOnStartup = false;
					if (UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						foreach (var tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = TabBarItemParameter.Deserialize(FolderViewViewModel, tabArgsString);
							await NavigationHelpers.AddNewTabByParamAsync(FolderViewViewModel, tabArgs.InitialPageType, tabArgs.NavigationParameter);
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
                        await NavigationHelpers.AddNewTabByPathAsync(FolderViewViewModel, typeof(ShellPanesPage), path, true);
                    }
                }
				else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
					UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
				{
                    if (AppInstancesManager.Get(FolderViewViewModel).Count == 0)
                    {
                        foreach (var tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
                        {
                            var tabArgs = TabBarItemParameter.Deserialize(FolderViewViewModel, tabArgsString);
                            await NavigationHelpers.AddNewTabByParamAsync(FolderViewViewModel, tabArgs.InitialPageType, tabArgs.NavigationParameter);
                        }
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
                            await NavigationHelpers.AddNewTabByPathAsync(FolderViewViewModel, typeof(ShellPanesPage), path, true);
                        }
                    }
                    else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
                            UserSettingsService.GeneralSettingsService.LastSessionTabList is not null &&
                            AppInstancesManager.Get(FolderViewViewModel).Count == 0)
                    {
                        foreach (var tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
						{
							var tabArgs = TabBarItemParameter.Deserialize(FolderViewViewModel, tabArgsString);
							await NavigationHelpers.AddNewTabByParamAsync(FolderViewViewModel, tabArgs.InitialPageType, tabArgs.NavigationParameter);
						}
					}
				}
				catch { }
			}

			if (parameter is string navArgs)
            {
                await NavigationHelpers.AddNewTabByPathAsync(FolderViewViewModel, typeof(ShellPanesPage), navArgs, true);
            }
            else if (parameter is PaneNavigationArguments paneArgs)
            {
                await NavigationHelpers.AddNewTabByParamAsync(FolderViewViewModel, typeof(ShellPanesPage), paneArgs);
            }
            else if (parameter is TabBarItemParameter tabArgs)
            {
                await NavigationHelpers.AddNewTabByParamAsync(FolderViewViewModel, tabArgs.InitialPageType, tabArgs.NavigationParameter);
            }
        }

        // CHANGE: Remove theme resource loading.
        /*// Load the app theme resources
        ResourcesService.LoadAppResources(AppearanceSettingsService);*/

        await Task.WhenAll(
            DrivesViewModel.UpdateDrivesAsync(),
            NetworkService.UpdateComputersAsync(),
            NetworkService.UpdateShortcutsAsync());
    }

    // Command methods

    private async void ExecuteNavigateToNumberedTabKeyboardAcceleratorCommand(KeyboardAcceleratorInvokedEventArgs? e)
    {
        var AppInstances = AppInstancesManager.Get(FolderViewViewModel);
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
            FolderViewViewModel.TabStripSelectedIndex = indexToSelect;

            // Small delay for the UI to load
            await Task.Delay(500);

            // Refocus on the file list
            (SelectedTabItem?.TabItemContent as Control)?.Focus(FocusState.Programmatic);
        }

        e.Handled = true;
    }
}
