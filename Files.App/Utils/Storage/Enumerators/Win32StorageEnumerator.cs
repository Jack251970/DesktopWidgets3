// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Services.SizeProvider;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Vanara.PInvoke;
using Windows.Storage;
using static Files.Core.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.Utils.Storage;

public static class Win32StorageEnumerator
{
    /*private static readonly ISizeProvider folderSizeProvider = DependencyExtensions.GetService<ISizeProvider>();*/

    private static readonly string folderTypeTextLocalized = "Folder".GetLocalizedResource();

    private static readonly IStorageCacheController fileListCache = StorageCacheController.GetInstance();

    public static async Task<List<ListedItem>> ListEntries(
        IFolderViewViewModel folderViewViewModel,
        string path,
        IntPtr hFile,
        NativeFindStorageItemHelper.WIN32_FIND_DATA findData,
        int countLimit,
        Func<List<ListedItem>, Task> intermediateAction,
        Dictionary<string, BitmapImage> defaultIconPairs,
        CancellationToken cancellationToken
    )
    {
        var sampler = new IntervalSampler(500);
        var tempList = new List<ListedItem>();
        var count = 0;

        var userSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
        var CalculateFolderSizes = userSettingsService.FoldersSettingsService.CalculateFolderSizes;

        var isGitRepo = GitHelpers.IsRepositoryEx(path, out var repoPath) && !string.IsNullOrEmpty((await GitHelpers.GetRepositoryHead(repoPath))?.Name);

        do
        {
            var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
            var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            var startWithDot = findData.cFileName.StartsWith('.');
            if ((!isHidden ||
                (userSettingsService.FoldersSettingsService.ShowHiddenItems &&
                (!isSystem || userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) &&
                (!startWithDot || userSettingsService.FoldersSettingsService.ShowDotFiles))
            {
                if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                {
                    var file = await GetFile(folderViewViewModel, findData, path, isGitRepo, cancellationToken);
                    if (file is not null)
                    {
                        if (defaultIconPairs is not null)
                        {
                            if (!string.IsNullOrEmpty(file.FileExtension))
                            {
                                var lowercaseExtension = file.FileExtension.ToLowerInvariant();
                                if (defaultIconPairs.TryGetValue(lowercaseExtension, out var value))
                                {
                                    file.FileImage = value;
                                }
                            }
                        }

                        tempList.Add(file);
                        ++count;

                        if (userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
                        {
                            tempList.AddRange(EnumAdsForPath(folderViewViewModel, file.ItemPath, file));
                        }
                    }
                }
                else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (findData.cFileName != "." && findData.cFileName != "..")
                    {
                        var folder = await GetFolder(folderViewViewModel, findData, path, isGitRepo, cancellationToken);
                        if (folder is not null)
                        {
                            if (defaultIconPairs?.ContainsKey(string.Empty) ?? false)
                            {
                                // Set folder icon (found by empty extension string)
                                folder.FileImage = defaultIconPairs[string.Empty];
                            }

                            tempList.Add(folder);
                            ++count;

                            if (userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
                            {
                                tempList.AddRange(EnumAdsForPath(folderViewViewModel, folder.ItemPath, folder));
                            }

                            if (CalculateFolderSizes)
                            {
                                var folderSizeProvider = folderViewViewModel.GetService<ISizeProvider>();
                                if (folderSizeProvider.TryGetSize(folder.ItemPath, out var size))
                                {
                                    folder.FileSizeBytes = (long)size;
                                    folder.FileSize = size.ToSizeString();
                                }

                                _ = folderSizeProvider.UpdateAsync(folder.ItemPath, cancellationToken);
                            }
                        }
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested || count == countLimit)
            {
                break;
            }

            if (intermediateAction is not null && (count == 32 || sampler.CheckNow()))
            {
                await intermediateAction(tempList);

                // clear the temporary list every time we do an intermediate action
                tempList.Clear();
            }
        } while (FindNextFile(hFile, out findData));

        FindClose(hFile);

        return tempList;
    }

    private static IEnumerable<ListedItem> EnumAdsForPath(IFolderViewViewModel folderViewViewModel, string itemPath, ListedItem main)
    {
        foreach (var ads in NativeFileOperationsHelper.GetAlternateStreams(itemPath))
        {
            yield return GetAlternateStream(folderViewViewModel, ads, main);
        }
    }

    public static ListedItem GetAlternateStream(IFolderViewViewModel folderViewViewModel, (string Name, long Size) ads, ListedItem main)
    {
        var itemType = "File".GetLocalizedResource();
        string itemFileExtension = null!;

        if (ads.Name.Contains('.'))
        {
            itemFileExtension = Path.GetExtension(ads.Name);
            itemType = itemFileExtension.Trim('.') + " " + itemType;
        }

        var adsName = ads.Name[1..^6]; // Remove ":" and ":$DATA"

        return new AlternateStreamItem(folderViewViewModel)
        {
            PrimaryItemAttribute = StorageItemTypes.File,
            FileExtension = itemFileExtension,
            FileImage = null!,
            LoadFileIcon = false,
            ItemNameRaw = adsName,
            IsHiddenItem = false,
            Opacity = Constants.UI.DimItemOpacity,
            ItemDateModifiedReal = main.ItemDateModifiedReal,
            ItemDateAccessedReal = main.ItemDateAccessedReal,
            ItemDateCreatedReal = main.ItemDateCreatedReal,
            ItemType = itemType,
            ItemPath = $"{main.ItemPath}:{adsName}",
            FileSize = ads.Size.ToSizeString(),
            FileSizeBytes = ads.Size
        };
    }

    public static async Task<ListedItem> GetFolder(
        IFolderViewViewModel folderViewViewModel,
        NativeFindStorageItemHelper.WIN32_FIND_DATA findData,
        string pathRoot,
        bool isGitRepo,
        CancellationToken cancellationToken
    )
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return null!;
        }

        DateTime itemModifiedDate;
        DateTime itemCreatedDate;

        try
        {
            FileTimeToSystemTime(ref findData.ftLastWriteTime, out var systemModifiedTimeOutput);
            itemModifiedDate = systemModifiedTimeOutput.ToDateTime();

            FileTimeToSystemTime(ref findData.ftCreationTime, out var systemCreatedTimeOutput);
            itemCreatedDate = systemCreatedTimeOutput.ToDateTime();
        }
        catch (ArgumentException)
        {
            // Invalid date means invalid findData, do not add to list
            return null!;
        }

        var itemPath = Path.Combine(pathRoot, findData.cFileName);

        var itemName = await fileListCache.ReadFileDisplayNameFromCache(itemPath, cancellationToken);
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = findData.cFileName;
        }

        var isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
        double opacity = 1;

        if (isHidden)
        {
            opacity = Constants.UI.DimItemOpacity;
        }

        if (isGitRepo)
        {
            return new GitItem(folderViewViewModel)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemNameRaw = itemName,
                ItemDateModifiedReal = itemModifiedDate,
                ItemDateCreatedReal = itemCreatedDate,
                ItemType = folderTypeTextLocalized,
                FileImage = null!,
                IsHiddenItem = isHidden,
                Opacity = opacity,
                LoadFileIcon = false,
                ItemPath = itemPath,
                FileSize = null!,
                FileSizeBytes = 0,
            };
        }
        else
        {
            return new ListedItem(folderViewViewModel, null!)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemNameRaw = itemName,
                ItemDateModifiedReal = itemModifiedDate,
                ItemDateCreatedReal = itemCreatedDate,
                ItemType = folderTypeTextLocalized,
                FileImage = null!,
                IsHiddenItem = isHidden,
                Opacity = opacity,
                LoadFileIcon = false,
                ItemPath = itemPath,
                FileSize = null!,
                FileSizeBytes = 0,
            };
        }
    }

    public static async Task<ListedItem> GetFile(
        IFolderViewViewModel folderViewViewModel,
        NativeFindStorageItemHelper.WIN32_FIND_DATA findData,
        string pathRoot,
        bool isGitRepo,
        CancellationToken cancellationToken
    )
    {
        var itemPath = Path.Combine(pathRoot, findData.cFileName);
        var itemName = findData.cFileName;

        DateTime itemModifiedDate, itemCreatedDate, itemLastAccessDate;

        try
        {
            FileTimeToSystemTime(ref findData.ftLastWriteTime, out var systemModifiedDateOutput);
            itemModifiedDate = systemModifiedDateOutput.ToDateTime();

            FileTimeToSystemTime(ref findData.ftCreationTime, out var systemCreatedDateOutput);
            itemCreatedDate = systemCreatedDateOutput.ToDateTime();

            FileTimeToSystemTime(ref findData.ftLastAccessTime, out var systemLastAccessOutput);
            itemLastAccessDate = systemLastAccessOutput.ToDateTime();
        }
        catch (ArgumentException)
        {
            // Invalid date means invalid findData, do not add to list
            return null!;
        }

        var itemSizeBytes = findData.GetSize();
        var itemSize = itemSizeBytes.ToSizeString();
        var itemType = "File".GetLocalizedResource();
        string itemFileExtension = null!;

        if (findData.cFileName.Contains('.'))
        {
            itemFileExtension = Path.GetExtension(itemPath);
            itemType = itemFileExtension.Trim('.') + " " + itemType;
        }

        var itemThumbnailImgVis = false;
        var itemEmptyImgVis = true;

        if (cancellationToken.IsCancellationRequested)
        {
            return null!;
        }

        var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
        double opacity = isHidden ? Constants.UI.DimItemOpacity : 1;

        // https://learn.microsoft.com/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4
        var isReparsePoint = ((FileAttributes)findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        var isSymlink = isReparsePoint && findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK;

        if (isSymlink)
        {
            var targetPath = NativeFileOperationsHelper.ParseSymLink(itemPath);

            return new ShortcutItem(folderViewViewModel, null!)
            {
                PrimaryItemAttribute = StorageItemTypes.File,
                FileExtension = itemFileExtension,
                IsHiddenItem = isHidden,
                Opacity = opacity,
                FileImage = null!,
                LoadFileIcon = itemThumbnailImgVis,
                LoadWebShortcutGlyph = false,
                ItemNameRaw = itemName,
                ItemDateModifiedReal = itemModifiedDate,
                ItemDateAccessedReal = itemLastAccessDate,
                ItemDateCreatedReal = itemCreatedDate,
                ItemType = "Shortcut".GetLocalizedResource(),
                ItemPath = itemPath,
                FileSize = itemSize,
                FileSizeBytes = itemSizeBytes,
                TargetPath = targetPath,
                IsSymLink = true
            };
        }
        else if (FileExtensionHelpers.IsShortcutOrUrlFile(findData.cFileName))
        {
            var isUrl = FileExtensionHelpers.IsWebLinkFile(findData.cFileName);

            var shInfo = await FileOperationsHelpers.ParseLinkAsync(itemPath);
            if (shInfo is null)
            {
                return null!;
            }

            return new ShortcutItem(folderViewViewModel, null!)
            {
                PrimaryItemAttribute = shInfo.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File,
                FileExtension = itemFileExtension,
                IsHiddenItem = isHidden,
                Opacity = opacity,
                FileImage = null!,
                LoadFileIcon = !shInfo.IsFolder && itemThumbnailImgVis,
                LoadWebShortcutGlyph = !shInfo.IsFolder && isUrl && itemEmptyImgVis,
                ItemNameRaw = itemName,
                ItemDateModifiedReal = itemModifiedDate,
                ItemDateAccessedReal = itemLastAccessDate,
                ItemDateCreatedReal = itemCreatedDate,
                ItemType = isUrl ? "ShortcutWebLinkFileType".GetLocalizedResource() : "Shortcut".GetLocalizedResource(),
                ItemPath = itemPath,
                FileSize = itemSize,
                FileSizeBytes = itemSizeBytes,
                TargetPath = shInfo.TargetPath,
                Arguments = shInfo.Arguments,
                WorkingDirectory = shInfo.WorkingDirectory,
                RunAsAdmin = shInfo.RunAsAdmin,
                IsUrl = isUrl,
            };
        }
        else if (App.LibraryManager.TryGetLibrary(itemPath, out var library))
        {
            return new LibraryItem(folderViewViewModel, library)
            {
                Opacity = opacity,
                ItemDateModifiedReal = itemModifiedDate,
                ItemDateCreatedReal = itemCreatedDate,
            };
        }
        else
        {
            if (ZipStorageFolder.IsZipPath(itemPath) && await ZipStorageFolder.CheckDefaultZipApp(itemPath))
            {
                return new ZipItem(folderViewViewModel, null!)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder, // Treat zip files as folders
                    FileExtension = itemFileExtension,
                    FileImage = null!,
                    LoadFileIcon = itemThumbnailImgVis,
                    ItemNameRaw = itemName,
                    IsHiddenItem = isHidden,
                    Opacity = opacity,
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateAccessedReal = itemLastAccessDate,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = itemSizeBytes
                };
            }
            else if (isGitRepo)
            {
                return new GitItem(folderViewViewModel)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    FileImage = null!,
                    LoadFileIcon = itemThumbnailImgVis,
                    ItemNameRaw = itemName,
                    IsHiddenItem = isHidden,
                    Opacity = opacity,
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateAccessedReal = itemLastAccessDate,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = itemSizeBytes
                };
            }
            else
            {
                return new ListedItem(folderViewViewModel, null!)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    FileImage = null!,
                    LoadFileIcon = itemThumbnailImgVis,
                    ItemNameRaw = itemName,
                    IsHiddenItem = isHidden,
                    Opacity = opacity,
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateAccessedReal = itemLastAccessDate,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = itemSizeBytes
                };
            }
        }
    }
}
