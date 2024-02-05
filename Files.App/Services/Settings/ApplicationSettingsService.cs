// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.Settings;

internal sealed class ApplicationSettingsService : BaseObservableJsonSettings, IApplicationSettingsService
{
    /*public ApplicationSettingsService(ISettingsSharingContext settingsSharingContext)
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
}
