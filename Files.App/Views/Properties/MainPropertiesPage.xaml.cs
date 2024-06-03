// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Graphics;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using Microsoft.UI.Input;

namespace Files.App.Views.Properties;

public sealed partial class MainPropertiesPage : BasePropertiesPage
{
    private AppWindow AppWindow => Window.AppWindow;

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

        Window = parameter.Window;

        // CHANGE: Set title bar to TitlebarArea.
        Window.SetTitleBar(TitlebarArea);

		base.OnNavigatedTo(e);

        MainPropertiesViewModel = new(FolderViewViewModel, Window, MainContentFrame, BaseProperties, parameter);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
	{
        // CHANGE: Register theme mode change event in BlankWindow.
        /*AppSettings = DependencyExtensions.GetService<SettingsViewModel>();
		AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;*/
        Window.Closed += Window_Closed;

		UpdatePageLayout();
        Window.RaiseSetTitleBarDragRegion(SetTitleBarDragRegion);
        Window.AppWindow.Changed += AppWindow_Changed;
    }

    private int SetTitleBarDragRegion(InputNonClientPointerSource source, SizeInt32 size, double scaleFactor, Func<UIElement, RectInt32?, RectInt32> getScaledRect)
    {
        source.SetRegionRects(NonClientRegionKind.Passthrough, [getScaledRect(BackwardNavigationButton, null)]);
        return (int)TitlebarArea.ActualHeight;
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
		=> UpdatePageLayout();

	private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key.Equals(VirtualKey.Escape))
        {
            await WindowsExtensions.CloseWindow(Window);
        }
    }

	private void UpdatePageLayout()
	{
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
        Window.AppWindow.Changed -= AppWindow_Changed;

        if (MainPropertiesViewModel.ChangedPropertiesCancellationTokenSource is not null &&
			!MainPropertiesViewModel.ChangedPropertiesCancellationTokenSource.IsCancellationRequested)
		{
			MainPropertiesViewModel.ChangedPropertiesCancellationTokenSource.Cancel();
		}
	}

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs e)
    {
        Window.RaiseSetTitleBarDragRegion(SetTitleBarDragRegion);
    }

    public async override Task<bool> SaveChangesAsync()
        => await Task.FromResult(false);

    public override void Dispose()
	{
	}
}
