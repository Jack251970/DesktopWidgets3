namespace DesktopWidgets3.Widget;

/// <summary>
/// Widget settings changed event arguments.
/// </summary>
public class WidgetSettingsChangedArgs
{
    /// <summary>
    /// Context of the widget instance or the widget setting instance.
    /// </summary>
    public required IBaseWidgetContext WidgetContext { get; init; }

    /// <summary>
    /// Settings of the widget instance.
    /// </summary>
    public required BaseWidgetSettings Settings { get; init; }
}
