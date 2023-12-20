// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers;

public static class PathNormalization
{
    public static string GetParentDir(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var index = path.Contains('/', StringComparison.Ordinal) ? path.LastIndexOf("/", StringComparison.Ordinal) : path.LastIndexOf("\\", StringComparison.Ordinal);
        return path[..(index != -1 ? index : path.Length)];
    }

    public static string Combine(string folder, string name)
    {
        return string.IsNullOrEmpty(folder)
            ? name
            : folder.Contains('/', StringComparison.Ordinal) ? Path.Combine(folder, name).Replace("\\", "/", StringComparison.Ordinal) : Path.Combine(folder, name);
    }
}
