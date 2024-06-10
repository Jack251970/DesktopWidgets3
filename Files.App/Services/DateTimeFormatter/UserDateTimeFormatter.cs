// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.DateTimeFormatter;

internal sealed class UserDateTimeFormatter : IDateTimeFormatter
{
    public IUserSettingsService UserSettingsService { get; private set; } = null!;

    private IDateTimeFormatter formatter = null!;

	public string Name
		=> formatter.Name;

	public UserDateTimeFormatter()
	{
        /*UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;*/

        Update();
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        UserSettingsService = folderViewViewModel.GetRequiredService<IUserSettingsService>();

        UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

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
        var dateTimeFormat = UserSettingsService is null ? DateTimeFormats.Application : UserSettingsService.GeneralSettingsService.DateTimeFormat;
        var factory = DependencyExtensions.GetRequiredService<IDateTimeFormatterFactory>();

		formatter = factory.GetDateTimeFormatter(dateTimeFormat);
	}

	private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
	{
		if (e.SettingName is nameof(UserSettingsService.GeneralSettingsService.DateTimeFormat))
        {
            Update();
        }
    }
}
