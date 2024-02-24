using System.Text;

namespace DesktopWidgets3.Core.Services;

public class FileService : IFileService
{
    private SaveTaskParameters lastSaveTaskParameter = new();

    public T Read<T>(string folderPath, string fileName, JsonSerializerSettings jsonSerializerSettings = null!)
    {
        Check(folderPath, fileName);

        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings)!;
        }

        return default!;
    }

    public async Task Save<T>(string folderPath, string fileName, T content, bool indent)
    {
        Check(folderPath, fileName);

        // save only if input parameters is different from last time
        var saveTaskParameter = new SaveTaskParameters
        {
            Type = typeof(T),
            FolderPath = folderPath,
            FileName = fileName,
            Content = content!,
            Indent = indent
        };
        if (lastSaveTaskParameter.Equals(saveTaskParameter))
        {
            return;
        }
        lastSaveTaskParameter = saveTaskParameter;

        await Task.Yield();

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonConvert.SerializeObject(content, indent ? Formatting.Indented : Formatting.None);
        File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public async Task Delete(string folderPath, string fileName)
    {
        Check(folderPath, fileName);

        await Task.Yield();

        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }

    private static void Check(string folderPath, string fileName)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));
        }

        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }
    }

    private class SaveTaskParameters
    {
        public Type Type { get; set; } = null!;
        public string FolderPath { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public object Content { get; set; } = default!;
        public bool Indent { get; set; } = false;

        public override bool Equals(object? obj)
        {
            if (obj is SaveTaskParameters parameters)
            {
                return Type == parameters.Type
                    && FolderPath == parameters.FolderPath
                    && FileName == parameters.FileName
                    && Content == parameters.Content
                    && Indent == parameters.Indent;
            }

            return false;
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
