// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace Files.App.Services;

public class AppThemeModeService : IAppThemeModeService
{
    // CHANGE: Remove theme setting and use theme extensions instead.
    /*private IUserSettingsService UserSettingsService { get; } = DependencyExtensions.GetService<IUserSettingsService>();

	private UISettings UISettings { get; } = new();

    /// <inheritdoc/>
    public ElementTheme AppThemeMode
	{
		get
		{
			var theme = UserSettingsService.AppearanceSettingsService.AppThemeMode;

			return EnumExtensions.GetEnum<ElementTheme>(theme);
		}
		set
		{
			UserSettingsService.AppearanceSettingsService.AppThemeMode = value.ToString();

			SetAppThemeMode();
		}
	}*/
    private ElementTheme appThemeMode;
    public ElementTheme AppThemeMode
    {
        get => appThemeMode;
        set
        {
            if (appThemeMode == value)
            {
                return;
            }

            appThemeMode = value;
            AppThemeModeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    public event EventHandler? AppThemeModeChanged;

	/// <summary>
	/// Initializes an instance of <see cref="AppThemeModeService"/>.
	/// </summary>
	public AppThemeModeService()
	{
        // CHANGE: Remove theme setting and use theme extensions instead.
        /*// Set the desired theme based on what is set in the application settings
		SetAppThemeMode();

		// Registering to color changes, so that we can notice when changed system theme mode
		UISettings.ColorValuesChanged += UISettings_ColorValuesChanged;*/
        appThemeMode = ThemeExtensions.RootTheme;
        ThemeExtensions.ElementTheme_Changed += (sender, theme) =>
        {
            AppThemeMode = theme;
        };
    }

	/// <inheritdoc/>
	public void ApplyResources()
	{
        // Toggle between the themes to force reload the resource styles
        // TODO(Later): Add theme resource reloading function.
        SetAppThemeMode(null, null, ElementTheme.Dark);
		SetAppThemeMode(null, null, ElementTheme.Light);

		// Restore the theme to the correct one
		SetAppThemeMode();
	}

	/// <inheritdoc/>
	public void SetAppThemeMode(Window? window = null, AppWindowTitleBar? titleBar = null, ElementTheme? rootTheme = null, bool callThemeModeChangedEvent = true)
	{
		window ??= WindowsExtensions.MainWindow;
		titleBar ??= WindowsExtensions.MainWindow.AppWindow.TitleBar;
		rootTheme ??= AppThemeMode;

		if (window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = (ElementTheme)rootTheme;
        }

        if (titleBar is not null)
		{
			titleBar.ButtonBackgroundColor = Colors.Transparent;
			titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			switch (rootTheme)
			{
				case ElementTheme.Default:
					titleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
					titleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
					break;
				case ElementTheme.Light:
					titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
					titleBar.ButtonForegroundColor = Colors.Black;
					break;
				case ElementTheme.Dark:
					titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
					titleBar.ButtonForegroundColor = Colors.White;
					break;
			}
		}

		if (callThemeModeChangedEvent)
        {
            AppThemeModeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    // CHANGE: Remove theme setting and use theme extensions instead.
    /*private async void UISettings_ColorValuesChanged(UISettings sender, object args)
	{
		await ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(() =>
		{
			SetAppThemeMode();
		});
	}*/
}
