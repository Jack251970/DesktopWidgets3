// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Enums;

namespace Files.Core.Services.DateTimeFormatter;

public interface IDateTimeFormatterFactory
{
    IDateTimeFormatter GetDateTimeFormatter(DateTimeFormats dateTimeFormat);
}