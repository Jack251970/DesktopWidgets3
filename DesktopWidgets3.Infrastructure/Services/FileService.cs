using System.Collections.Concurrent;

namespace DesktopWidgets3.Infrastructure.Services;

public class FileService : IFileService
{
    private static string ClassName => typeof(FileService).Name;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphoreSlims = [];

    public T Read<T>(string folderPath, string fileName, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        var path = GetPath(folderPath, fileName);
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings)!;
            }
            catch (Exception e)
            {
                LogExtensions.LogError(ClassName, e, $"Reading file {path} failed");
            }
        }

        return default!;
    }

    public async Task<T> ReadAsync<T>(string folderPath, string fileName, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        return await Task.Run(() => Read<T>(folderPath, fileName, jsonSerializerSettings));
    }

    public async Task<string?> SaveAsync<T>(string folderPath, string fileName, T content, bool indent)
    {
        var path = GetPath(folderPath, fileName, true);

        semaphoreSlims.TryGetValue(path, out var semaphoreSlim);
        if (semaphoreSlim == null)
        {
            semaphoreSlim = new SemaphoreSlim(1);
            semaphoreSlims.TryAdd(path, semaphoreSlim);
        }

        await semaphoreSlim.WaitAsync();

        var fileContent = JsonConvert.SerializeObject(content, indent ? Formatting.Indented : Formatting.None);
        try
        {
            File.WriteAllText(path, fileContent, System.Text.Encoding.UTF8);
        }
        catch (Exception e)
        {
            LogExtensions.LogError(ClassName, e, $"Writing file {path} failed");
        }
        finally
        {
            semaphoreSlim.Release();
        }

        return fileContent;
    }

    public bool Delete(string folderPath, string fileName)
    {
        var path = GetPath(folderPath, fileName);
        if (fileName != null && File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                LogExtensions.LogError(ClassName, e, $"Deleting file {path} failed");
            }
            return true;
        }

        return false;
    }

    private static string GetPath(string folderPath, string fileName, bool createDirectory = false)
    {
        if (createDirectory && (!Directory.Exists(folderPath)))
        {
            Directory.CreateDirectory(folderPath);
        }

        return Path.Combine(folderPath, fileName);
    }
}
