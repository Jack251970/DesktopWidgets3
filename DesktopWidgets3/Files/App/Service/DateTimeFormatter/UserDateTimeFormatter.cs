// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Enums;
using Files.Core.Services.DateTimeFormatter;

namespace Files.App.Services.DateTimeFormatter;

internal class UserDateTimeFormatter : IDateTimeFormatter
{
    private readonly IDateTimeFormatterFactory factory;

    private IDateTimeFormatter formatter = null!;

    public string Name => formatter.Name;

    public UserDateTimeFormatter()
    {
        factory = DesktopWidgets3.App.GetService<IDateTimeFormatterFactory>();

        Update();
        //TODO: Add Callback of settings
        //UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
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

        formatter = factory.GetDateTimeFormatter(dateTimeFormat);
    }

    /*private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
    {
        if (e.SettingName is nameof(UserSettingsService.GeneralSettingsService.DateTimeFormat))
            Update();
    }*/
}