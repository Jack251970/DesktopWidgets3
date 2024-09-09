namespace DesktopWidgets3.Utils;

public static class StringUtils
{
    public static string GetGuid()
    {
        return Guid.NewGuid().ToString("N").ToUpper();
    }
}
