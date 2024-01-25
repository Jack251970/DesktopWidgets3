// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.Settings;

// TODO: change to internal.
public sealed class ApplicationSettingsService : BaseObservableJsonSettings, IApplicationSettingsService
{
	public bool ClickedToReviewApp
	{
		get => Get(false);
		set => Set(value);
	}
		
	public bool ShowRunningAsAdminPrompt
	{
		get => Get(true);
		set => Set(value);
	}

	public ApplicationSettingsService(ISettingsSharingContext settingsSharingContext)
	{
		RegisterSettingsContext(settingsSharingContext);
	}
}
