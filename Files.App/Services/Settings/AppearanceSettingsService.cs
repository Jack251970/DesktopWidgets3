// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Services.Settings;

internal sealed class AppearanceSettingsService : BaseObservableJsonSettings, IAppearanceSettingsService
{
    /*public AppearanceSettingsService(ISettingsSharingContext settingsSharingContext)
	{
		// Register root
		RegisterSettingsContext(settingsSharingContext);
	}*/

    public void Initialize(IUserSettingsService userSettingsService)
    {
        // Register root
        var settingsSharingContext = ((UserSettingsService)userSettingsService).GetSharingContext();
        RegisterSettingsContext(settingsSharingContext);
    }

    public double SidebarWidth
	{
		get => Get(Math.Min(Math.Max(Get(255d), Constants.UI.MinimumSidebarWidth), 500d));
		set => Set(value);
	}

	public bool IsSidebarOpen
	{
		get => Get(true);
		set => Set(value);
	}

    /// <inheritdoc/>
    public string AppThemeMode
    {
        get => Get("Default")!;
        set => Set(value);
    }

    /// <inheritdoc/>
    public string AppThemeBackgroundColor
	{
		get => Get("#00000000")!;
		set => Set(value);
	}

	/// <inheritdoc/>
	public string AppThemeAddressBarBackgroundColor
	{
		get => Get("")!;
		set => Set(value);
	}

    /// <inheritdoc/>
    public string AppThemeToolbarBackgroundColor
    {
        get => Get("")!;
        set => Set(value);
    }

    /// <inheritdoc/>
    public string AppThemeSidebarBackgroundColor
	{
		get => Get("")!;
		set => Set(value);
	}

	/// <inheritdoc/>
	public string AppThemeFileAreaBackgroundColor
	{
		get => Get("")!;
		set => Set(value);
	}

    /// <inheritdoc/>
    public string AppThemeFileAreaSecondaryBackgroundColor
    {
        get => Get("")!;
        set => Set(value);
    }

    /// <inheritdoc/>
    public string AppThemeInfoPaneBackgroundColor
    {
        get => Get("")!;
        set => Set(value);
    }

    /// <inheritdoc/>
    public string AppThemeFontFamily
	{
		get => Get("Segoe UI Variable")!;
		set => Set(value);
	}

	/// <inheritdoc/>
	public BackdropMaterialType AppThemeBackdropMaterial
	{
		get => Get(BackdropMaterialType.MicaAlt);
		set => Set(value);
	}

    /// <inheritdoc/>
    public string AppThemeBackgroundImageSource
    {
        get => Get("")!;
        set => Set(value);
    }

    /// <inheritdoc/>
    public Stretch AppThemeBackgroundImageFit
    {
        get => Get(Stretch.UniformToFill);
        set => Set(value);
    }

    /// <inheritdoc/>
    public float AppThemeBackgroundImageOpacity
    {
        get => Get(1f);
        set => Set(value);
    }

    /// <inheritdoc/>
    public VerticalAlignment AppThemeBackgroundImageVerticalAlignment
    {
        get => Get(VerticalAlignment.Center);
        set => Set(value);
    }

    /// <inheritdoc/>
    public HorizontalAlignment AppThemeBackgroundImageHorizontalAlignment
    {
        get => Get(HorizontalAlignment.Center);
        set => Set(value);
    }

    /// <inheritdoc/>
    public bool ShowToolbar
    {
        get => Get(true);
        set => Set(value);
    }

    protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
    {
        base.RaiseOnSettingChangedEvent(sender, e);
    }
}
