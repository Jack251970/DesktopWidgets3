namespace DesktopWidgets3.Widget;

/// <summary>
/// Context to provide information for one widget instance.
/// </summary>
public interface IWidgetContext : IBaseWidgetContext
{
    /// <summary>
    /// The activation state of the widget instance.
    /// </summary>
    public bool IsActive { get; }
}
