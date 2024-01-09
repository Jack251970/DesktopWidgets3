// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;

namespace DesktopWidgets3.Files.App.Services.DateTimeFormatter;

internal class UniversalDateTimeFormatter : AbstractDateTimeFormatter
{
    public override string Name
        => "Universal".GetLocalized();

    public override string ToShortLabel(DateTimeOffset offset)
    {
        if (offset.Year is <= 1601 or >= 9999)
        {
            return " ";
        }

        return ToString(offset, "yyyy-MM-dd HH:mm:ss");
    }
}