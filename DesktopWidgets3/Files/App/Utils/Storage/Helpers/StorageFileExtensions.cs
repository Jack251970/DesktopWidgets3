// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Immutable;
using System.Text;
using DesktopWidgets3.Helpers;
using Files.App.Data.Items;
using Files.App.Data.Models;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Utils.Shell;
using Files.Shared.Extensions;
using Windows.Storage;
using Windows.Storage.Search;
using DesktopWidgets3.ViewModels.Pages.Widget;

namespace Files.App.Utils.Storage.Helpers;

public static class StorageFileExtensions
{
    private const int SINGLE_DOT_DIRECTORY_LENGTH = 2;
    private const int DOUBLE_DOT_DIRECTORY_LENGTH = 3;

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

    public static async Task<List<IStorageItem>> ToStandardStorageItemsAsync(this IEnumerable<IStorageItem> items)
    {
        var newItems = new List<IStorageItem>();
        foreach (var item in items)
        {
            try
            {
                if (item is null)
                {
                }
                else if (item.IsOfType(StorageItemTypes.File))
                {
                    newItems.Add(await item.AsBaseStorageFile()!.ToStorageFileAsync());
                }
                else if (item.IsOfType(StorageItemTypes.Folder))
                {
                    newItems.Add(await item.AsBaseStorageFolder()!.ToStorageFolderAsync());
                }
            }
            catch (NotSupportedException)
            {
                // Ignore items that can't be converted
            }
        }
        return newItems;
    }

    public static bool AreItemsInSameDrive(this IEnumerable<string> itemsPath, string destinationPath)
    {
        try
        {
            var destinationRoot = Path.GetPathRoot(destinationPath);
            return itemsPath.Any(itemPath => Path.GetPathRoot(itemPath)!.Equals(destinationRoot, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
    public static bool AreItemsInSameDrive(this IEnumerable<IStorageItem> storageItems, string destinationPath)
        => storageItems.Select(x => x.Path).AreItemsInSameDrive(destinationPath);
    public static bool AreItemsInSameDrive(this IEnumerable<IStorageItemWithPath> storageItems, string destinationPath)
        => storageItems.Select(x => x.Path).AreItemsInSameDrive(destinationPath);

    public static bool AreItemsAlreadyInFolder(this IEnumerable<string> itemsPath, string destinationPath)
    {
        try
        {
            var trimmedPath = destinationPath.TrimPath();
            return itemsPath.All(itemPath => Path.GetDirectoryName(itemPath)!.Equals(trimmedPath, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
    public static bool AreItemsAlreadyInFolder(this IEnumerable<IStorageItem> storageItems, string destinationPath)
        => storageItems.Select(x => x.Path).AreItemsAlreadyInFolder(destinationPath);
    public static bool AreItemsAlreadyInFolder(this IEnumerable<IStorageItemWithPath> storageItems, string destinationPath)
        => storageItems.Select(x => x.Path).AreItemsAlreadyInFolder(destinationPath);

    public static BaseStorageFolder? AsBaseStorageFolder(this IStorageItem item)
    {
        if (item is not null && item.IsOfType(StorageItemTypes.Folder))
        {
            return item is StorageFolder folder ? (BaseStorageFolder)folder : item as BaseStorageFolder;
        }

        return null;
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

    public static string GetResolvedPath(FolderViewViewModel viewModel,string path, bool isFtp)
    {
        var withoutEnvirnment = GetPathWithoutEnvironmentVariable(path);
        return ResolvePath(viewModel, withoutEnvirnment, isFtp);
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
    public static async Task<IList<StorageFileWithPath>> GetFilesWithPathAsync
            (this StorageFolderWithPath parentFolder, uint maxNumberOfItems = uint.MaxValue)
                => (await parentFolder.Item.GetFilesAsync(CommonFileQuery.DefaultQuery, 0, maxNumberOfItems))
                    .Select(x => new StorageFileWithPath(x, string.IsNullOrEmpty(x.Path) ? PathNormalization.Combine(parentFolder.Path, x.Name) : x.Path)).ToList();

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

    public static async Task<IList<StorageFolderWithPath>> GetFoldersWithPathAsync
            (this StorageFolderWithPath parentFolder, uint maxNumberOfItems = uint.MaxValue)
                => (await parentFolder.Item.GetFoldersAsync(CommonFolderQuery.DefaultQuery, 0, maxNumberOfItems))
                    .Select(x => new StorageFolderWithPath(x, string.IsNullOrEmpty(x.Path) ? PathNormalization.Combine(parentFolder.Path, x.Name) : x.Path)).ToList();
    public static async Task<IList<StorageFolderWithPath>> GetFoldersWithPathAsync
        (this StorageFolderWithPath parentFolder, string nameFilter, uint maxNumberOfItems = uint.MaxValue)
    {
        if (parentFolder is null)
        {
            return null!;
        }

        var queryOptions = new QueryOptions
        {
            ApplicationSearchFilter = $"System.FileName:{nameFilter}*"
        };
        var queryResult = parentFolder.Item.CreateFolderQueryWithOptions(queryOptions);

        return (await queryResult.GetFoldersAsync(0, maxNumberOfItems))
            .Select(x => new StorageFolderWithPath(x, string.IsNullOrEmpty(x.Path) ? PathNormalization.Combine(parentFolder.Path, x.Name) : x.Path)).ToList();
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

    private static string GetPathWithoutEnvironmentVariable(string path)
    {
        if (path.StartsWith("~\\", StringComparison.Ordinal))
        {
            path = $"{Constants.UserEnvironmentPaths.HomePath}{path.Remove(0, 1)}";
        }

        path = path.Replace("%temp%", Constants.UserEnvironmentPaths.TempPath, StringComparison.OrdinalIgnoreCase);

        path = path.Replace("%tmp%", Constants.UserEnvironmentPaths.TempPath, StringComparison.OrdinalIgnoreCase);

        path = path.Replace("%localappdata%", Constants.UserEnvironmentPaths.LocalAppDataPath, StringComparison.OrdinalIgnoreCase);

        path = path.Replace("%homepath%", Constants.UserEnvironmentPaths.HomePath, StringComparison.OrdinalIgnoreCase);

        return Environment.ExpandEnvironmentVariables(path);
    }

    private static string ResolvePath(FolderViewViewModel viewModel, string path, bool isFtp)
    {
        /*if (path.StartsWith("Home"))
        {
            return "Home";
        }*/

        if (ShellStorageFolder.IsShellPath(path))
        {
            return ShellHelpers.ResolveShellPath(path);
        }

        var pathBuilder = new StringBuilder(path);
        var lastPathIndex = path.Length - 1;
        var separatorChar = isFtp || path.Contains('/', StringComparison.Ordinal) ? '/' : '\\';
        var rootIndex = isFtp ? FtpHelpers.GetRootIndex(path) + 1 : path.IndexOf($":{separatorChar}", StringComparison.Ordinal) + 2;

        for (int i = 0, lastIndex = 0; i < pathBuilder.Length; i++)
        {
            if (pathBuilder[i] is not '?' &&
                pathBuilder[i] != Path.DirectorySeparatorChar &&
                pathBuilder[i] != Path.AltDirectorySeparatorChar &&
                i != lastPathIndex)
            {
                continue;
            }

            if (lastIndex == i)
            {
                ++lastIndex;
                continue;
            }

            var component = pathBuilder.ToString()[lastIndex..i];
            if (component is "..")
            {
                if (lastIndex is 0)
                {
                    SetCurrentWorkingDirectory(viewModel, pathBuilder, separatorChar, lastIndex, ref i);
                }
                else if (lastIndex == rootIndex)
                {
                    pathBuilder.Remove(lastIndex, DOUBLE_DOT_DIRECTORY_LENGTH);
                    i = lastIndex - 1;
                }
                else
                {
                    var directoryIndex = pathBuilder.ToString().LastIndexOf(
                        separatorChar,
                        lastIndex - DOUBLE_DOT_DIRECTORY_LENGTH);

                    if (directoryIndex is not -1)
                    {
                        pathBuilder.Remove(directoryIndex, i - directoryIndex);
                        i = directoryIndex;
                    }
                }

                lastPathIndex = pathBuilder.Length - 1;
            }
            else if (component is ".")
            {
                if (lastIndex is 0)
                {
                    SetCurrentWorkingDirectory(viewModel, pathBuilder, separatorChar, lastIndex, ref i);
                }
                else
                {
                    pathBuilder.Remove(lastIndex, SINGLE_DOT_DIRECTORY_LENGTH);
                    i -= 3;
                }
                lastPathIndex = pathBuilder.Length - 1;
            }

            lastIndex = i + 1;
        }

        return pathBuilder.ToString();
    }

    private static void SetCurrentWorkingDirectory(FolderViewViewModel viewModel, StringBuilder path, char separator, int substringIndex, ref int i)
    {
        var subPath = path.ToString()[substringIndex..];

        path.Clear();
        path.Append(viewModel.FileSystemViewModel.WorkingDirectory);
        path.Append(separator);
        path.Append(subPath);
        i = -1;
    }
}
