namespace DesktopWidgets3.Widget;

/// <summary>
/// Base context to provide information for one widget instance or one widget setting instance.
/// </summary>
public interface IBaseWidgetContext
{
    /// <summary>
    /// The unique identifier of the widget instance or the widget setting instance.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The widget type of the widget instance or the widget setting instance.
    /// </summary>
    public string Type { get; }
}
