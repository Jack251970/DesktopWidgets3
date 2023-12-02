﻿using Newtonsoft.Json;

namespace DesktopWidgets3.Core.Contracts.Services;

public interface IFileService
{
    T Read<T>(string folderPath, string fileName, JsonConverter jsonConverter = null);

    void Save<T>(string folderPath, string fileName, T content, bool indent);

    void Delete(string folderPath, string fileName);
}
