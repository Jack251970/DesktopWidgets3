// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.DateTimeFormatter;

// TODO: Add AppLifeCycleHelper.cs and change to internal.
public class UserDateTimeFormatter : IDateTimeFormatter
{
    /*public IUserSettingsService UserSettingsService { get; } = DependencyExtensions.GetService<IUserSettingsService>();*/

    private IDateTimeFormatter formatter = null!;

	public string Name
		=> formatter.Name;

	public UserDateTimeFormatter()
	{
        // TODO: Add Callback of settings.
        /*UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;*/

        Update();
	}

	public string ToShortLabel(DateTimeOffset offset)
		=> formatter.ToShortLabel(offset);

	public string ToLongLabel(DateTimeOffset offset)
		=> formatter.ToLongLabel(offset);

	public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset, GroupByDateUnit unit)
		=> formatter.ToTimeSpanLabel(offset, unit);

	private void Update()
	{
        // TODO: Add UserSettingsService.GeneralSettingsService.DateTimeFormat
        var dateTimeFormat = DateTimeFormats.Application;
        var factory = DependencyExtensions.GetService<IDateTimeFormatterFactory>();

		formatter = factory.GetDateTimeFormatter(dateTimeFormat);
	}

	/*private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
	{
		if (e.SettingName is nameof(UserSettingsService.GeneralSettingsService.DateTimeFormat))
        {
            Update();
        }
    }*/
}
