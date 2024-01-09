// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.Core.Data.Enums;

namespace DesktopWidgets3.Files.Core.Services.DateTimeFormatter;

public interface IDateTimeFormatterFactory
{
    IDateTimeFormatter GetDateTimeFormatter(DateTimeFormats dateTimeFormat);
}