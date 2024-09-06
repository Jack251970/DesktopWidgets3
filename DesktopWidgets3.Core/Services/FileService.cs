using System.Text;

namespace DesktopWidgets3.Core.Services;

public class FileService : IFileService
{
    private readonly Dictionary<string, SaveTaskParameters> lastSaveTaskParameters = [];

    public T Read<T>(string folderPath, string fileName, JsonSerializerSettings jsonSerializerSettings = null!)
    {
        var path = CheckPath(folderPath, fileName);

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings)!;
        }

        return default!;
    }

    public async Task<string?> Save<T>(string folderPath, string fileName, T content, bool indent, bool ignoreIfSameToLast = false)
    {
        await Task.Yield();

        var path = CheckPath(folderPath, fileName);

        var fileContent = JsonConvert.SerializeObject(content, indent ? Formatting.Indented : Formatting.None);
        
        if (ignoreIfSameToLast)
        {
            // save only if input parameters is different from last time
            var saveTaskParameter = new SaveTaskParameters
            {
                Type = typeof(T),
                FolderPath = folderPath,
                FileName = fileName,
                Content = fileContent,
                Indent = indent
            };
            if (IsParametersSame<T>(path, saveTaskParameter))
            {
                return null;
            }
        }

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        try
        {
            File.WriteAllText(path, fileContent, Encoding.UTF8);
        }
        catch (Exception)
        {
            // Retry?
            return null;
        }

        return fileContent;
    }

    public async Task<bool> Delete(string folderPath, string fileName)
    {
        await Task.Yield();

        var path = CheckPath(folderPath, fileName);

        if (fileName != null && File.Exists(path))
        {
            File.Delete(path);
            return true;
        }

        return false;
    }

    private static string CheckPath(string folderPath, string fileName)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));
        }

        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return Path.Combine(folderPath, fileName);
    }

    private bool IsParametersSame<T>(string path, SaveTaskParameters saveTaskParameter)
    {
        if (lastSaveTaskParameters.TryGetValue(path, out var par))
        {
            lastSaveTaskParameters[path] = saveTaskParameter;
            return par == saveTaskParameter;
        }

        lastSaveTaskParameters[path] = saveTaskParameter;  // TODO: Fix System.NullReferenceException: 'Object reference not set to an instance of an object.'
        return false;
    }

    private class SaveTaskParameters
    {
        public Type Type { get; set; } = null!;
        public string FolderPath { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string Content { get; set; } = default!;
        public bool Indent { get; set; } = false;

        public static bool operator ==(SaveTaskParameters obj1, SaveTaskParameters obj2)
        {
            if (obj1 is null && obj2 is null)
            {
                return true;
            }

            if (obj1 is null || obj2 is null)
            {
                return false;
            }

            return obj1.Type == obj2.Type
                && obj1.FolderPath == obj2.FolderPath
                && obj1.FileName == obj2.FileName
                && obj1.Content == obj2.Content
                && obj1.Indent == obj2.Indent;
        }

        public static bool operator !=(SaveTaskParameters obj1, SaveTaskParameters obj2)
        {
            return !(obj1 == obj2);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (obj is SaveTaskParameters arg)
            {
                return arg == this;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode()
                ^ FolderPath.GetHashCode()
                ^ FileName.GetHashCode()
                ^ Content.GetHashCode()
                ^ Indent.GetHashCode();
        }
    }
}
