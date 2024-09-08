namespace DesktopWidgets3.Widget.Models.Setting;

public class BaseWidgetSettings
{
    public virtual BaseWidgetSettings Clone()
    {
        return (BaseWidgetSettings)MemberwiseClone();
    }
}
