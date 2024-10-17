namespace DesktopWidgets3.Widget;

/// <summary>
/// The base widget settings model.
/// </summary>
public class BaseWidgetSettings
{
    /// <summary>
    /// Clone the widget settings.
    /// If shallow copy is not enough, override this method.
    /// </summary>
    /// <returns>A new instance of the widget settings.</returns>
    public virtual BaseWidgetSettings Clone()
    {
        return (BaseWidgetSettings)MemberwiseClone();
    }
}
