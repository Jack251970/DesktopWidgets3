namespace DesktopWidgets3.Widget;

/// <summary>
/// Context to provide information for one widget instance.
/// </summary>
public interface IWidgetContext
{
    /// <summary>
    /// The unique identifier of the widget instance.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The widget type of the widget instance.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The activation state of the widget instance.
    /// </summary>
    public bool IsActive { get; }
}
