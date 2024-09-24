namespace DesktopWidgets3.Helpers;

public class ConstantHelper
{
#if DEBUG
    public static string AppAppDisplayName => "AppDisplayName".GetLocalized() + " (Debug)";
#else
    public static string AppAppDisplayName => "AppDisplayName".GetLocalized();
#endif
}
