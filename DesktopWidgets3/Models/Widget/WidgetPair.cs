﻿namespace DesktopWidgets3.Models.Widget;

public class WidgetPair
{
    public WidgetMetadata Metadata { get; internal set; } = null!;

    public IAsyncWidget Widget { get; internal set; } = null!;

    public override bool Equals(object? obj)
    {
        if (obj is WidgetPair widgetPair)
        {
            return string.Equals(widgetPair.Metadata.ID, Metadata.ID);
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        var hashcode = Metadata.ID?.GetHashCode() ?? 0;
        return hashcode;
    }

    public override string ToString()
    {
        return Metadata.Name;
    }
}
