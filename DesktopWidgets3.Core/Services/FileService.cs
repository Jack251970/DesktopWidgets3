using System.Text;

namespace DesktopWidgets3.Core.Services;

public class FileService : IFileService
{
    private readonly Dictionary<string, SemaphoreSlim> semaphoreSlims = [];

    public T Read<T>(string folderPath, string fileName, JsonSerializerSettings jsonSerializerSettings = null!)
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
                LogExtensions.LogError(e, $"Reading file {path} failed");
            }
        }

        return default!;
    }

    public async Task<string?> Save<T>(string folderPath, string fileName, T content, bool indent)
    {   
        var path = GetPath(folderPath, fileName, true);

        semaphoreSlims.TryGetValue(path, out var semaphoreSlim);
        if (semaphoreSlim == null)
        {
            semaphoreSlim = new SemaphoreSlim(1);
            semaphoreSlims.Add(path, semaphoreSlim);
        }

        await semaphoreSlim.WaitAsync();

        var fileContent = JsonConvert.SerializeObject(content, indent ? Formatting.Indented : Formatting.None);
        try
        {
            File.WriteAllText(path, fileContent, Encoding.UTF8);
        }
        catch (Exception e)
        {
            LogExtensions.LogError(e, $"Writing file {path} failed");
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
                LogExtensions.LogError(e, $"Deleting file {path} failed");
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
