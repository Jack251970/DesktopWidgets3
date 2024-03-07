// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace Files.App.Views.Properties;

public sealed partial class MainPropertiesPage : BasePropertiesPage
{
    private AppWindow AppWindow = null!;

	private Window Window = null!;

    // CHANGE: Move app settings to BlankWindow.
	/*private SettingsViewModel AppSettings { get; set; } = null!;*/

    private MainPropertiesViewModel MainPropertiesViewModel { get; set; } = null!;

	public MainPropertiesPage()
	{
		InitializeComponent();

		if (FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft)
        {
            FlowDirection = FlowDirection.RightToLeft;
        }
    }

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		var parameter = (PropertiesPageNavigationParameter)e.Parameter;

        AppWindow = parameter.AppWindow;
		Window = parameter.Window;

        // CHANGE: Set title bar to TitlebarArea.
        Window.SetTitleBar(TitlebarArea);

		base.OnNavigatedTo(e);

		MainPropertiesViewModel = new(FolderViewViewModel, Window, AppWindow, MainContentFrame, BaseProperties, parameter);
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
        // CHANGE: Register theme mode change event in BlankWindow.
        /*AppSettings = DependencyExtensions.GetService<SettingsViewModel>();
		AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;*/
        Window.Closed += Window_Closed;

		UpdatePageLayout();
	}

	private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
		=> UpdatePageLayout();

	private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key.Equals(VirtualKey.Escape))
        {
            await UIElementExtensions.CloseWindow(Window);
        }
    }

	private void UpdatePageLayout()
	{
        // CHANGE: Remove drag zone change event.
        /*// Drag zone
		DragZoneHelper.SetDragZones(Window, (int)TitlebarArea.ActualHeight, 40);*/

        // NavigationView Pane Mode
        MainPropertiesWindowNavigationView.PaneDisplayMode =
			ActualWidth <= 600
				? NavigationViewPaneDisplayMode.LeftCompact
				: NavigationViewPaneDisplayMode.Left;

		// Collapse NavigationViewItem Content text
		if (ActualWidth <= 600)
        {
            foreach (var item in MainPropertiesViewModel.NavigationViewItems)
            {
                item.IsCompact = true;
            }
        }
        else
        {
            foreach (var item in MainPropertiesViewModel.NavigationViewItems)
            {
                item.IsCompact = false;
            }
        }
    }

    // CHANGE: Remove theme mode change event.
	/*private async void AppSettings_ThemeModeChanged(object? sender, EventArgs e)
	{
		if (Parent is null)
        {
            return;
        }

        await DispatcherQueue.EnqueueOrInvokeAsync(() =>
		{
			((Frame)Parent).RequestedTheme = ThemeHelper.RootTheme;

			switch (ThemeHelper.RootTheme)
			{
				case ElementTheme.Default:
					AppWindow.TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
					AppWindow.TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
					break;
				case ElementTheme.Light:
					AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x33, 0, 0, 0);
					AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
					break;
				case ElementTheme.Dark:
					AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF);
					AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
					break;
			}
		});
	}*/

	private void Window_Closed(object sender, WindowEventArgs args)
	{
		/*AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;*/
		Window.Closed -= Window_Closed;

		if (MainPropertiesViewModel.ChangedPropertiesCancellationTokenSource is not null &&
			!MainPropertiesViewModel.ChangedPropertiesCancellationTokenSource.IsCancellationRequested)
		{
			MainPropertiesViewModel.ChangedPropertiesCancellationTokenSource.Cancel();
		}
	}

	public async override Task<bool> SaveChangesAsync()
		=> await Task.FromResult(false);

	public override void Dispose()
	{
	}
}
