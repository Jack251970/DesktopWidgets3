namespace DesktopWidgets3.Widget;

public interface ILocalizationService
{
    string GetLocalizedString(string key);

    string GetLocalizedString(string key, params object[] args);
}
