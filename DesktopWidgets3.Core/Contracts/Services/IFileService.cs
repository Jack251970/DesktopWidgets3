namespace DesktopWidgets3.Core.Contracts.Services;

public interface IFileService
{
    T Read<T>(string folderPath, string fileName, JsonSerializerSettings jsonSerializerSettings = null!);

    Task<string?> Save<T>(string folderPath, string fileName, T content, bool indent, bool ignoreIfEqualLast = false);

    Task<bool> Delete(string folderPath, string fileName);
}
