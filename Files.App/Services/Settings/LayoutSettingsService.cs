// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.Settings;

internal sealed class LayoutSettingsService : BaseObservableJsonSettings, ILayoutSettingsService
{
	/*public LayoutSettingsService(ISettingsSharingContext settingsSharingContext)
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

    public int DefaultGridViewSize
	{
		get => (int)Get((long)Constants.Browser.GridViewBrowser.GridViewSizeSmall);
		set => Set((long)value);
	}
}
