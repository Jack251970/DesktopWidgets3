namespace DesktopWidgets3.Widget;

/// <summary>
/// Context to provide information for one widget setting instance.
/// </summary>
public interface IWidgetSettingContext : IBaseWidgetContext
{
    /// <summary>
    /// The navigate state of the widget setting instance.
    /// </summary>
    public bool IsNavigated { get; }
}
