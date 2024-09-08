namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IPublicAPIService
{
    T LoadSettingJsonStorage<T>() where T : new();

    void SaveSettingJsonStorage<T>() where T : new();
}
