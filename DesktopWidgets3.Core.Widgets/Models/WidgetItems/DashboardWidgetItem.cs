using Microsoft.UI.Xaml.Media;

namespace DesktopWidgets3.Core.Widgets.Models.WidgetItems;

public class DashboardWidgetGroupItem : BaseWidgetGroupItem
{
    public required string Name { get; set; }

    public required string IcoPath { get; set; }

    public required List<string> Types { get; set; }
}

public class DashboardWidgetItem : BaseWidgetItem
{
    public required WidgetProviderType ProviderType { get; set; }

    public required string Name { get; set; }

    public required Brush IconFill { get; set; }

    public new bool Pinned
    {
        get => _pinned;
        set
        {
            if (_pinned != value)
            {
                _pinned = value;
                if (Editable)
                {
                    PinnedChangedCallback?.Invoke(this);
                }
            }
        }
    }

    public required bool IsUnknown { get; set; }

    public required bool IsInstalled { get; set; }

    public bool Editable => !IsUnknown && IsInstalled;

    public Action<DashboardWidgetItem>? PinnedChangedCallback { get; set; }

    public bool Equals(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        return ProviderType == providerType && Id == widgetId && Type == widgetType && Index == widgetIndex;
    }
}
