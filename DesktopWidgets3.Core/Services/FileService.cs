using System.Text;
using Newtonsoft.Json;
using DesktopWidgets3.Core.Contracts.Services;

namespace DesktopWidgets3.Core.Services;

public class FileService : IFileService
{
    public T Read<T>(string folderPath, string fileName, JsonSerializerSettings jsonSerializerSettings = null)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings)!;
        }

        return default!;
    }

    public void Save<T>(string folderPath, string fileName, T content, bool indent)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonConvert.SerializeObject(content, indent ? Formatting.Indented : Formatting.None);
        File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }
}
