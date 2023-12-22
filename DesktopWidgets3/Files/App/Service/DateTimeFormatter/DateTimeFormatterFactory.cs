// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Enums;
using Files.Core.Services.DateTimeFormatter;

namespace Files.App.Services.DateTimeFormatter;

public class DateTimeFormatterFactory : IDateTimeFormatterFactory
{
    public IDateTimeFormatter GetDateTimeFormatter(DateTimeFormats dateTimeFormat) => dateTimeFormat switch
    {
        DateTimeFormats.Application => new ApplicationDateTimeFormatter(),
        DateTimeFormats.System => new SystemDateTimeFormatter(),
        DateTimeFormats.Universal => new UniversalDateTimeFormatter(),
        _ => throw new ArgumentException(null, nameof(dateTimeFormat)),
    };
}
