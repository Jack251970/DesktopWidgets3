using System.IO;

namespace Files.App.Data.Exceptions;

public sealed class FileAlreadyExistsException(string message, string fileName) : IOException(message)
{
    public string FileName { get; private set; } = fileName;
}
