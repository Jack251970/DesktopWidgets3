namespace DesktopWidgets3.Core.Widgets.Models.WidgetPairs;

public class WidgetGroupPair
{
    public WidgetGroupMetadata Metadata { get; internal set; } = null!;

    public IExtensionAssembly ExtensionAssembly { get; internal set; } = null!;

    public IAsyncWidgetGroup WidgetGroup { get; internal set; } = null!;

    public override bool Equals(object? obj)
    {
        if (obj is WidgetGroupPair widgetPair)
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
