namespace DesktopWidgets3.Helpers;

public class ConstantHelper
{
#if DEBUG
    public static readonly string AppAppDisplayName = "AppDisplayName".GetLocalized() + " (Debug)";
#else
    public static readonly string AppAppDisplayName = "AppDisplayName".GetLocalized();
#endif
}
