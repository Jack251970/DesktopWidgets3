namespace DesktopWidgets3.Infrastructure.Contracts.Services;

public interface IFileService
{
    Task<T> ReadAsync<T>(string folderPath, string fileName, JsonSerializerSettings? jsonSerializerSettings = null);

    Task<string?> SaveAsync<T>(string folderPath, string fileName, T content, bool indent);

    T Read<T>(string folderPath, string fileName, JsonSerializerSettings? jsonSerializerSettings = null);

    Task<string?> Save<T>(string folderPath, string fileName, T content, bool indent);

    bool Delete(string folderPath, string fileName);
}
