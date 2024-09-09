namespace DesktopWidgets3.Models.Widget;

public static class AllowedLanguage
{
    public const string CSharp = "CSharp";

    public const string FSharp = "FSharp";

    public static bool IsDotNet(string language)
    {
        return language.Equals(CSharp, StringComparison.OrdinalIgnoreCase) 
            || language.Equals(FSharp, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAllowed(string language)
    {
        return IsDotNet(language);
    }
}
