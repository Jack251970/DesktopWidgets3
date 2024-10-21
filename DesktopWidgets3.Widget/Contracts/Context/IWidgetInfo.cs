namespace DesktopWidgets3.Widget;

/// <summary>
/// Information of one widget instance.
/// </summary>
public interface IWidgetInfo
{
    /// <summary>
    /// Context to provide information for one widget instance.
    /// </summary>
    public IWidgetContext WidgetContext { get; }

    /// <summary>
    /// The settings of the widget instance.
    /// </summary>
    public BaseWidgetSettings Settings { get; }
}
