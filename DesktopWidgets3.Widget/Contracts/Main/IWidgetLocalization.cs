namespace DesktopWidgets3.Widget.Contracts.Main;

/// <summary>
/// Represent widgets that support localization.
/// </summary>
public interface IWidgetLocalization
{
    /// <summary>
    /// Get a localized widget title
    /// </summary>
    string GetLocalizatedTitle();

    /// <summary>
    /// Get a localized widget description
    /// </summary>
    string GetLocalizatedDescription();
}
