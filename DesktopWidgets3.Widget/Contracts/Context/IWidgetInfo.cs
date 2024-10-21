namespace DesktopWidgets3.Widget;

/// <summary>
/// Information of one widget.
/// </summary>
public interface IWidgetInfo
{
    /// <summary>
    /// Context to provide information for the widget.
    /// </summary>
    public IWidgetContext WidgetContext { get; }

    /// <summary>
    /// Settings of the widget.
    /// </summary>
    public BaseWidgetSettings WidgetSettings { get; }
}
