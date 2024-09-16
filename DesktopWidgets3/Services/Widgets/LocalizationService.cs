namespace DesktopWidgets3.Services.Widgets;

internal class LocalizationService : ILocalizationService
{
    public string AssemblyName { get; internal set; } = string.Empty;

    public string GetLocalizedString(string key) => AssemblyName != string.Empty ? key.GetLocalized(AssemblyName) : string.Empty;
}
