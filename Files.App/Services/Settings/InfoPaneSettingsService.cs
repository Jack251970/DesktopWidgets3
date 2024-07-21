// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.Settings;

internal sealed class InfoPaneSettingsService : BaseObservableJsonSettings, IInfoPaneSettingsService
{
    /*public InfoPaneSettingsService(ISettingsSharingContext settingsSharingContext)
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

    public bool IsEnabled
	{
		get => Get(false);
		set => Set(value);
	}

	public double HorizontalSizePx
	{
		get => Math.Max(100d, Get(300d));
		set => Set(Math.Max(100d, value));
	}

	public double VerticalSizePx
	{
		get => Math.Max(100d, Get(250d));
		set => Set(Math.Max(100d, value));
	}

	public double MediaVolume
	{
		get => Math.Min(Math.Max(Get(1d), 0d), 1d);
		set => Set(Math.Max(0d, Math.Min(value, 1d)));
	}

	public InfoPaneTabs SelectedTab
	{
		get => Get(InfoPaneTabs.Details);
		set => Set(value);
	}

	protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
	{
		base.RaiseOnSettingChangedEvent(sender, e);
	}
}
