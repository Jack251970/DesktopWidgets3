namespace DesktopWidgets3.Services;

internal class PublicAPIService : IPublicAPIService
{
    public T LoadSettingJsonStorage<T>() where T : new()
    {
        return new T();
    }

    public void SaveSettingJsonStorage<T>() where T : new()
    {

    }
}
