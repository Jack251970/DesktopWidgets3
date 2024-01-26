// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Utils.Shell;
using Files.App.Utils;
using Files.App.Utils.Storage;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Core.Helpers;
using Files.Shared.Helpers;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Files.App.Data.EventArguments;
using DesktopWidgets3.Helpers;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.Helpers;

public static class NavigationHelpers
{
    public static async Task OpenSelectedItemsAsync(FolderViewViewModel viewModel, bool openViaApplicationPicker = false)
    {
        // Don't open files and folders inside recycle bin
        if (viewModel.FileSystemViewModel.WorkingDirectory.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal) ||
            viewModel.SelectedItems is null)
        {
            return;
        }

        var forceOpenInExplore = false;
        var selectedItems = viewModel.SelectedItems.ToList();
        var opened = false;

        // If multiple files are selected, open them together
        if (!openViaApplicationPicker && selectedItems.Count > 1 &&
            selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && !x.IsExecutable && !x.IsShortcut))
        {
            opened = await Win32Helpers.InvokeWin32ComponentAsync(string.Join('|', selectedItems.Select(x => x.ItemPath)), viewModel);
        }

        if (opened)
        {
            return;
        }

        var folderCount = selectedItems.Count(item => item.PrimaryItemAttribute == StorageItemTypes.Folder);
        forceOpenInExplore = folderCount > 1;
        foreach (var item in selectedItems)
        {
            var type = item.PrimaryItemAttribute == StorageItemTypes.Folder 
                ? FilesystemItemType.Directory
                : FilesystemItemType.File;

            await OpenPath(viewModel, item.ItemPath, type, false, openViaApplicationPicker, forceOpenInExplore: forceOpenInExplore);
        }
    }

    public static async Task OpenItemsWithExecutableAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItemWithPath> items, string executablePath)
    {
        // Don't open files and folders inside recycle bin
        if (viewModel.FileSystemViewModel.WorkingDirectory.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal) ||
            viewModel is null)
        {
            return;
        }

        var arguments = string.Join(" ", items.Select(item => $"\"{item.Path}\""));
        await Win32Helpers.InvokeWin32ComponentAsync(executablePath, viewModel, arguments);
    }

    public static async Task<bool> OpenPath(FolderViewViewModel viewModel, string path, FilesystemItemType? itemType = null, bool openSilent = false, bool openViaApplicationPicker = false, IEnumerable<string>? selectItems = null, string? args = default, bool forceOpenInExplore = false)
    {
        var previousDir = viewModel.FileSystemViewModel.WorkingDirectory;
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
        var isDirectory = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory);
        var isReparsePoint = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.ReparsePoint);
        var isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(path);
        var isScreenSaver = FileExtensionHelpers.IsScreenSaverFile(path);
        /*var isTag = path.StartsWith("tag:");*/
        var opened = (FilesystemResult)false;

        /*if (isTag)
        {
            if (!forceOpenInNewTab)
            {
                associatedInstance.NavigateToPath(path, new NavigationArguments()
                {
                    IsSearchResultPage = true,
                    SearchPathParam = "Home",
                    SearchQuery = path,
                    AssociatedTabInstance = associatedInstance,
                    NavPathParam = path
                });
            }
            else
            {
                await NavigationHelpers.OpenPathInNewTab(path);
            }

            return true;
        }*/

        var shortcutInfo = new ShellLinkItem();
        if (itemType is null || isShortcut || isHiddenItem || isReparsePoint)
        {
            if (isShortcut)
            {
                var shInfo = await FileOperationsHelpers.ParseLinkAsync(path);

                if (shInfo is null)
                {
                    return false;
                }

                itemType = shInfo.IsFolder ? FilesystemItemType.Directory : FilesystemItemType.File;

                shortcutInfo = shInfo;

                if (shortcutInfo.InvalidTarget)
                {
                    if (await DialogDisplayHelper.ShowDialogAsync(viewModel, DynamicDialogFactory.GetFor_ShortcutNotFound(shortcutInfo.TargetPath!)) != DynamicDialogResult.Primary)
                    {
                        return false;
                    }

                    // Delete shortcut
                    var shortcutItem = StorageHelpers.FromPathAndType(path, FilesystemItemType.File);
                    await DependencyExtensions.GetService<IFileSystemHelpers>().DeleteItemAsync(viewModel, shortcutItem, DeleteConfirmationPolicies.Never, false);
                }
            }
            else if (isReparsePoint)
            {
                if (!isDirectory && NativeFindStorageItemHelper.GetWin32FindDataForPath(path, out var findData) &&
                    findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK)
                {
                    shortcutInfo.TargetPath = NativeFileOperationsHelper.ParseSymLink(path);
                }
                itemType ??= isDirectory ? FilesystemItemType.Directory : FilesystemItemType.File;
            }
            else if (isHiddenItem)
            {
                itemType = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory) ? FilesystemItemType.Directory : FilesystemItemType.File;
            }
            else
            {
                itemType = await StorageHelpers.GetTypeFromPath(path);
            }
        }

        switch (itemType)
        {
            case FilesystemItemType.Library:
                opened = await OpenLibrary(viewModel, path, selectItems, forceOpenInExplore);
                break;

            case FilesystemItemType.Directory:
                opened = await OpenDirectory(viewModel, path, selectItems, shortcutInfo, forceOpenInExplore);
                break;

            case FilesystemItemType.File:
                // Starts the screensaver in full-screen mode
                if (isScreenSaver)
                {
                    args += "/s";
                }

                opened = await OpenFile(viewModel, path, shortcutInfo, openViaApplicationPicker, args);
                break;
        };

        if (opened.ErrorCode == FileSystemStatusCode.NotFound)
        {
            if (!openSilent)
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "FileNotFoundDialog/Title".ToLocalized(), "FileNotFoundDialog/Text".ToLocalized());
            }
            viewModel.ToolbarViewModel.CanRefresh = false;
            await viewModel.FileSystemViewModel.RefreshItems(previousDir);
        }

        return opened;
    }

    private static async Task<FilesystemResult> OpenLibrary(FolderViewViewModel viewModel, string path, IEnumerable<string>? selectItems, bool forceOpenInExplore)
    {
        var opened = (FilesystemResult)false;
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden);
        if (isHiddenItem)
        {
            var allowNavigation = viewModel.GetSettings().AllowNavigation;
            await OpenPath(viewModel, forceOpenInExplore, allowNavigation, path);
            opened = (FilesystemResult)true;
        }
        /*else if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
        {
            opened = (FilesystemResult)await library.CheckDefaultSaveFolderAccess();
            if (opened)
            {
                await OpenPathAsync(viewModel, forceOpenInExplore, path, library.Text, selectItems);
            }
        }*/
        return opened;
    }

    private static async Task<FilesystemResult> OpenDirectory(FolderViewViewModel viewModel, string path, IEnumerable<string>? selectItems, ShellLinkItem shortcutInfo, bool forceOpenInExplore)
    {
        var opened = (FilesystemResult)false;
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden);
        var isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(path);
        var allowNavigation = viewModel.GetSettings().AllowNavigation;

        if (isShortcut)
        {
            if (string.IsNullOrEmpty(shortcutInfo.TargetPath))
            {
                await Win32Helpers.InvokeWin32ComponentAsync(path, viewModel);
                opened = (FilesystemResult)true;
            }
            else
            {
                await OpenPath(viewModel, forceOpenInExplore, allowNavigation, shortcutInfo.TargetPath, selectItems);
                opened = (FilesystemResult)true;
            }
        }
        else if (isHiddenItem)
        {
            await OpenPath(viewModel, forceOpenInExplore, allowNavigation, path);
            opened = (FilesystemResult)true;
        }
        else
        {
            opened = await viewModel.FileSystemViewModel.GetFolderWithPathFromPathAsync(path)
                .OnSuccess((childFolder) => {});
            if (!opened)
            {
                opened = (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path);
            }

            if (opened)
            {
                await OpenPath(viewModel, forceOpenInExplore, allowNavigation, path, selectItems);
            }
            else
            {
                await Win32Helpers.InvokeWin32ComponentAsync(path, viewModel);
            }
        }
        return opened;
    }

    private static async Task<FilesystemResult> OpenFile(FolderViewViewModel viewModel, string path, ShellLinkItem shortcutInfo, bool openViaApplicationPicker = false, string? args = default)
    {
        var opened = (FilesystemResult)false;
        var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Hidden);
        var isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(path) || !string.IsNullOrEmpty(shortcutInfo.TargetPath);

        if (isShortcut)
        {
            if (string.IsNullOrEmpty(shortcutInfo.TargetPath))
            {
                await Win32Helpers.InvokeWin32ComponentAsync(path, viewModel, args!);
            }
            else
            {
                if (!FileExtensionHelpers.IsWebLinkFile(path))
                {
                    StorageFileWithPath childFile = await viewModel.FileSystemViewModel.GetFileWithPathFromPathAsync(shortcutInfo.TargetPath);
                }
                await Win32Helpers.InvokeWin32ComponentAsync(shortcutInfo.TargetPath, viewModel, $"{args} {shortcutInfo.Arguments}", shortcutInfo.RunAsAdmin, shortcutInfo.WorkingDirectory!);
            }
            opened = (FilesystemResult)true;
        }
        else if (isHiddenItem)
        {
            await Win32Helpers.InvokeWin32ComponentAsync(path, viewModel, args!);
        }
        else
        {
            opened = await viewModel.FileSystemViewModel.GetFileWithPathFromPathAsync(path)
                .OnSuccess(async childFile =>
                {
                    if (openViaApplicationPicker)
                    {
                        var options = InitializeWithWindow(viewModel, new LauncherOptions
                        {
                            DisplayApplicationPicker = true
                        });
                        if (!await Launcher.LaunchFileAsync(childFile.Item, options))
                        {
                            await ContextMenu.InvokeVerb("openas", path);
                        }
                    }
                    else
                    {
                        //try using launcher first
                        var launchSuccess = false;

                        BaseStorageFileQueryResult? fileQueryResult = null;

                        //Get folder to create a file query (to pass to apps like Photos, Movies & TV..., needed to scroll through the folder like what Windows Explorer does)
                        BaseStorageFolder currentFolder = await viewModel.FileSystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(path));

                        if (currentFolder is not null)
                        {
                            QueryOptions queryOptions = new(CommonFileQuery.DefaultQuery, null);

                            //We can have many sort entries
                            SortEntry sortEntry = new()
                            {
                                AscendingOrder = true
                            };
                            // TODO: Add associatedInstance.InstanceViewModel.FolderSettings.DirectorySortDirection here
                            //associatedInstance.InstanceViewModel.FolderSettings.DirectorySortDirection == SortDirection.Ascending

                            //Basically we tell to the launched app to follow how we sorted the files in the directory.
                            var sortOption = SortOption.Name;
                            // TODO: Add associatedInstance.InstanceViewModel.FolderSettings.DirectorySortOption; here

                            switch (sortOption)
                            {
                                case SortOption.Name:
                                    sortEntry.PropertyName = "System.ItemNameDisplay";
                                    queryOptions.SortOrder.Clear();
                                    queryOptions.SortOrder.Add(sortEntry);
                                    break;

                                case SortOption.DateModified:
                                    sortEntry.PropertyName = "System.DateModified";
                                    queryOptions.SortOrder.Clear();
                                    queryOptions.SortOrder.Add(sortEntry);
                                    break;

                                case SortOption.DateCreated:
                                    sortEntry.PropertyName = "System.DateCreated";
                                    queryOptions.SortOrder.Clear();
                                    queryOptions.SortOrder.Add(sortEntry);
                                    break;

                                //Unfortunately this is unsupported | Remarks: https://learn.microsoft.com/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041
                                //case Enums.SortOption.Size:

                                //sortEntry.PropertyName = "System.TotalFileSize";
                                //queryOptions.SortOrder.Clear();
                                //queryOptions.SortOrder.Add(sortEntry);
                                //break;

                                //Unfortunately this is unsupported | Remarks: https://learn.microsoft.com/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041
                                //case Enums.SortOption.FileType:

                                //sortEntry.PropertyName = "System.FileExtension";
                                //queryOptions.SortOrder.Clear();
                                //queryOptions.SortOrder.Add(sortEntry);
                                //break;

                                //Handle unsupported
                                default:
                                    //keep the default one in SortOrder IList
                                    break;
                            }

                            var options = InitializeWithWindow(viewModel, new LauncherOptions());
                            if (currentFolder.AreQueryOptionsSupported(queryOptions))
                            {
                                fileQueryResult = currentFolder.CreateFileQueryWithOptions(queryOptions);
                                options.NeighboringFilesQuery = fileQueryResult.ToStorageFileQueryResult();
                            }

                            // Now launch file with options.
                            var storageItem = (StorageFile)await FilesystemTasks.Wrap(() => childFile.Item.ToStorageFileAsync().AsTask());

                            if (storageItem is not null)
                            {
                                launchSuccess = await Launcher.LaunchFileAsync(storageItem, options);
                            }
                        }

                        if (!launchSuccess)
                        {
                            await Win32Helpers.InvokeWin32ComponentAsync(path, viewModel, args!);
                        }
                    }
                });
        }
        return opened;
    }

    // WINUI3
    private static LauncherOptions InitializeWithWindow(FolderViewViewModel viewModel, LauncherOptions obj)
    {
        WinRT.Interop.InitializeWithWindow.Initialize(obj, viewModel.WidgetWindow.WindowHandle);
        return obj;
    }

    private static Task OpenPath(FolderViewViewModel viewModel, bool forceOpenInExplore, bool allowNavigation, string path, IEnumerable<string>? selectItems = null)
            => OpenPathAsync(viewModel, forceOpenInExplore, allowNavigation, path, path, selectItems);

    private static async Task OpenPathAsync(FolderViewViewModel viewModel, bool forceOpenInExplore, bool allowNavigation, string path, string text, IEnumerable<string>? selectItems = null)
    {
        if (forceOpenInExplore || !allowNavigation)
        {
            //await OpenPathInNewTab(text);
            FileSystemHelper.OpenInExplorer(path);
        }
        else
        {
            //associatedInstance.ToolbarViewModel.PathControlDisplayText = text;
            // TODO: Add associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(path) here
            viewModel.NavigateWithArguments(new NavigationArguments()
            {
                NavPathParam = path,
                SelectItems = selectItems,
                PushFolderPath = true,
                RefreshBehaviour = NavigationArguments.RefreshBehaviours.NavigateToPath
            });
        }
        await Task.CompletedTask;
    }
}