// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Immutable;
using DesktopWidgets3.Helpers;
using Files.App.Data.Items;
using Files.App.Data.Models;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Shared.Extensions;
using Windows.Storage;

namespace Files.App.Utils.Storage.Helpers;

public static class StorageFileExtensions
{
    public static readonly ImmutableHashSet<string> _ftpPaths = 
        new HashSet<string>() { "ftp:/", "ftps:/", "ftpes:/" }.ToImmutableHashSet();

    public static BaseStorageFile? AsBaseStorageFile(this IStorageItem item)
    {
        if (item is null || !item.IsOfType(StorageItemTypes.File))
        {
            return null;
        }

        return item is StorageFile file ? (BaseStorageFile)file : item as BaseStorageFile;
    }

    public static BaseStorageFolder? AsBaseStorageFolder(this IStorageItem item)
    {
        if (item is not null && item.IsOfType(StorageItemTypes.Folder))
        {
            return item is StorageFolder folder ? (BaseStorageFolder)folder : item as BaseStorageFolder;
        }

        return null;
    }

    public static async Task<BaseStorageFile> DangerousGetFileFromPathAsync(
        string path, 
        StorageFolderWithPath rootFolder = null!, 
        StorageFolderWithPath parentFolder = null!) 
        => (await DangerousGetFileWithPathFromPathAsync(path, rootFolder, parentFolder)).Item;
    public static async Task<StorageFileWithPath> DangerousGetFileWithPathFromPathAsync(
        string path, 
        StorageFolderWithPath rootFolder = null!, 
        StorageFolderWithPath parentFolder = null!)
    {
        if (rootFolder is not null)
        {
            var currComponents = GetDirectoryPathComponents(path);

            if (parentFolder is not null && path.IsSubPathOf(parentFolder.Path))
            {
                var folder = parentFolder.Item;
                var prevComponents = GetDirectoryPathComponents(parentFolder.Path);
                var _path = parentFolder.Path;
                foreach (var component in currComponents.ExceptBy(prevComponents, c => c.Path).SkipLast(1))
                {
                    folder = await folder.GetFolderAsync(component.Title!);
                    _path = PathNormalization.Combine(_path, folder.Name);
                }
                var file = await folder.GetFileAsync(currComponents.Last().Title!);
                _path = PathNormalization.Combine(_path, file.Name);
                return new StorageFileWithPath(file, _path);
            }
            else if (path.IsSubPathOf(rootFolder.Path))
            {
                var folder = rootFolder.Item;
                var _path = rootFolder.Path;
                foreach (var component in currComponents.Skip(1).SkipLast(1))
                {
                    folder = await folder.GetFolderAsync(component.Title!);
                    _path = PathNormalization.Combine(_path, folder.Name);
                }
                var file = await folder.GetFileAsync(currComponents.Last().Title!);
                _path = PathNormalization.Combine(_path, file.Name);
                return new StorageFileWithPath(file, _path);
            }
        }

        var fullPath = (parentFolder is not null && !FtpHelpers.IsFtpPath(path) && !Path.IsPathRooted(path) && !ShellStorageFolder.IsShellPath(path)) // "::{" not a valid root
            ? Path.GetFullPath(Path.Combine(parentFolder.Path, path)) // Relative path
            : path;
        var item = await BaseStorageFile.GetFileFromPathAsync(fullPath);

        if (parentFolder is not null && parentFolder.Item is IPasswordProtectedItem ppis && item is IPasswordProtectedItem ppid)
        {
            ppid.Credentials = ppis.Credentials;
        }

        return new StorageFileWithPath(item);
    }

    public static async Task<BaseStorageFolder> DangerousGetFolderFromPathAsync(
        string path, 
        StorageFolderWithPath rootFolder = null!, 
        StorageFolderWithPath parentFolder = null!) 
        => (await DangerousGetFolderWithPathFromPathAsync(path, rootFolder, parentFolder)).Item;
    public static async Task<StorageFolderWithPath> DangerousGetFolderWithPathFromPathAsync(
        string path, 
        StorageFolderWithPath rootFolder = null!, 
        StorageFolderWithPath parentFolder = null!)
    {
        if (rootFolder is not null)
        {
            var currComponents = GetDirectoryPathComponents(path);

            if (rootFolder.Path == path)
            {
                return rootFolder;
            }
            else if (parentFolder is not null && path.IsSubPathOf(parentFolder.Path))
            {
                var folder = parentFolder.Item;
                var prevComponents = GetDirectoryPathComponents(parentFolder.Path);
                var _path = parentFolder.Path;
                foreach (var component in currComponents.ExceptBy(prevComponents, c => c.Path))
                {
                    folder = await folder.GetFolderAsync(component.Title!);
                    _path = PathNormalization.Combine(_path, folder.Name);
                }
                return new StorageFolderWithPath(folder, _path);
            }
            else if (path.IsSubPathOf(rootFolder.Path))
            {
                var folder = rootFolder.Item;
                var _path = rootFolder.Path;
                foreach (var component in currComponents.Skip(1))
                {
                    folder = await folder.GetFolderAsync(component.Title!);
                    _path = PathNormalization.Combine(_path, folder.Name);
                }
                return new StorageFolderWithPath(folder, _path);
            }
        }

        var fullPath = (parentFolder is not null && !FtpHelpers.IsFtpPath(path) && !Path.IsPathRooted(path) && !ShellStorageFolder.IsShellPath(path)) // "::{" not a valid root
            ? Path.GetFullPath(Path.Combine(parentFolder.Path, path)) // Relative path
            : path;
        var item = await BaseStorageFolder.GetFolderFromPathAsync(fullPath);

        if (parentFolder is not null && parentFolder.Item is IPasswordProtectedItem ppis && item is IPasswordProtectedItem ppid)
        {
            ppid.Credentials = ppis.Credentials;
        }

        return new StorageFolderWithPath(item);
    }

    public static List<PathBoxItem> GetDirectoryPathComponents(string value)
    {
        List<PathBoxItem> pathBoxItems = new();

        if (value.Contains('/', StringComparison.Ordinal))
        {
            if (!value.EndsWith('/'))
            {
                value += "/";
            }
        }
        else if (!value.EndsWith('\\'))
        {
            value += "\\";
        }

        var lastIndex = 0;

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] is '?' || value[i] == Path.DirectorySeparatorChar || value[i] == Path.AltDirectorySeparatorChar)
            {
                if (lastIndex == i)
                {
                    ++lastIndex;
                    continue;
                }

                var component = value[lastIndex..i];
                var path = value[..(i + 1)];
                if (!_ftpPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    pathBoxItems.Add(GetPathItem(component, path));
                }

                lastIndex = i + 1;
            }
        }

        return pathBoxItems;
    }

    private static PathBoxItem GetPathItem(string component, string path)
    {
        string title;

        if (component.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
        {
            // Handle the recycle bin: use the localized folder name
            title = "RecycleBin".GetLocalized();
        }
        else if (component.StartsWith(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.Ordinal))
        {
            title = "ThisPC".GetLocalized();
        }
        else if (component.StartsWith(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.Ordinal))
        {
            title = "SidebarNetworkDrives".GetLocalized();
        }
        else if (component.Contains(':', StringComparison.Ordinal))
        {
            var drivesViewModel = DesktopWidgets3.App.GetService<DrivesViewModel>();

            var drives = drivesViewModel.Drives.Cast<DriveItem>();
            var drive = drives.FirstOrDefault(y => /*y.ItemType is NavigationControlItemType.Drive && */y.Path.Contains(component, StringComparison.OrdinalIgnoreCase));
            title = drive is not null ? drive.Text : string.Format("DriveWithLetter".GetLocalized(), component);
        }
        else
        {
            if (path.EndsWith('\\') || path.EndsWith('/'))
            {
                path = path.Remove(path.Length - 1);
            }

            title = component;
        }

        if (path.EndsWith('\\') || path.EndsWith('/'))
        {
            path = path.Remove(path.Length - 1);
        }

        title = component;

        return new PathBoxItem()
        {
            Title = title,
            Path = path
        };
    }
}
