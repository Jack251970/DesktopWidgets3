// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;

namespace DesktopWidgets3.Files.App.Services.DateTimeFormatter;

internal class SystemDateTimeFormatter : AbstractDateTimeFormatter
{
    public override string Name
        => "SystemTimeStyle".GetLocalized();

    public override string ToShortLabel(DateTimeOffset offset)
    {
        if (offset.Year is <= 1601 or >= 9999)
        {
            return " ";
        }

        return ToString(offset, "g");
    }
}
