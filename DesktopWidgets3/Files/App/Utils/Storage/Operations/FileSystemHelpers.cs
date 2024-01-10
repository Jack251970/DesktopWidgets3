// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using CommunityToolkit.Mvvm.DependencyInjection;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Helpers;
using Files.App.Utils.RecycleBin;
using Files.App.Utils.StatusCenter;
using Files.App.Utils.Storage.Helpers;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Core.Services;
using Files.Core.Storage;
using Files.Core.Storage.Extensions;
using Files.Core.ViewModels.Dialogs.FileSystemDialog;
using Files.Shared.Extensions;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.Utils.Storage;

public sealed class FileSystemHelpers : IFileSystemHelpers
{
    private IFileSystemOperations fileSystemOperations;

    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public FileSystemHelpers()
    {
        fileSystemOperations = new ShellFileSystemOperations();
    }

    #region Create

    public async Task<(ReturnResult, IStorageItem?)> CreateAsync(FolderViewViewModel viewModel, IStorageItemWithPath source)
    {
        var returnStatus = ReturnResult.InProgress;
        var progress = new Progress<StatusCenterItemProgressModel>();
        progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

        if (!IsValidForFilename(source.Name))
        {
            await DialogDisplayHelper.ShowDialogAsync(
                viewModel,
                "ErrorDialogThisActionCannotBeDone".GetLocalized(),
                "ErrorDialogNameNotAllowed".GetLocalized());
            return (ReturnResult.Failed, null);
        }

        var result = await fileSystemOperations.CreateAsync(viewModel, source, progress, cancellationToken);

        await Task.Yield();
        return (returnStatus, result);
    }

    #endregion

    #region Delete

    public async Task<ReturnResult> DeleteItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItemWithPath> source, DeleteConfirmationPolicies showDialog, bool permanently)
    {
        source = await source.ToListAsync();

        var returnStatus = ReturnResult.InProgress;

        var progress = new Progress<StatusCenterItemProgressModel>();
        progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

        var deleteFromRecycleBin = source.Select(item => item.Path).Any(RecycleBinHelpers.IsPathUnderRecycleBin);
        var canBeSentToBin = !deleteFromRecycleBin && await RecycleBinHelpers.HasRecycleBin(source.FirstOrDefault()?.Path);

        // Get the delete type from the dialog
        if (showDialog is DeleteConfirmationPolicies.Always ||
            showDialog is DeleteConfirmationPolicies.PermanentOnly &&
            (permanently || !canBeSentToBin))
        {
            var incomingItems = new List<BaseFileSystemDialogItemViewModel>();
            List<ShellFileItem>? binItems = null;

            foreach (var src in source)
            {
                if (RecycleBinHelpers.IsPathUnderRecycleBin(src.Path))
                {
                    binItems ??= await RecycleBinHelpers.EnumerateRecycleBin();

                    // Might still be null because we're deserializing the list from Json
                    if (!binItems.IsEmpty())
                    {
                        // Get original file name
                        var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == src.Path);
                        incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src.Path, DisplayName = matchingItem?.FileName ?? src.Name });
                    }
                }
                else
                {
                    incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src.Path });
                }
            }

            var dialogViewModel = FileSystemDialogViewModel.GetDialogViewModel(
                viewModel,
                new() { IsInDeleteMode = true },
                (!canBeSentToBin || permanently, canBeSentToBin),
                FileSystemOperationType.Delete,
                incomingItems,
                new());

            var dialogService = viewModel.DialogService;

            // Return if the result isn't delete
            if (await dialogService.ShowDialogAsync(dialogViewModel) != DialogResult.Primary)
            {
                return ReturnResult.Cancelled;
            }

            // Delete selected items if the result is Yes
            permanently = dialogViewModel.DeletePermanently;
        }
        else
        {
            // Delete permanently if recycle bin is not supported
            permanently |= !canBeSentToBin;
        }

        // Delete file
        await fileSystemOperations.DeleteItemsAsync(viewModel, (IList<IStorageItemWithPath>)source, progress, permanently, cancellationToken);
        
        return returnStatus;
    }

    public Task<ReturnResult> DeleteItemAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, DeleteConfirmationPolicies showDialog, bool permanently)
            => DeleteItemsAsync(viewModel, source.CreateEnumerable(), showDialog, permanently);

    public Task<ReturnResult> DeleteItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItem> source, DeleteConfirmationPolicies showDialog, bool permanently)
        => DeleteItemsAsync(viewModel, source.Select((item) => item.FromStorageItem()), showDialog, permanently);

    public Task<ReturnResult> DeleteItemAsync(FolderViewViewModel viewModel, IStorageItem source, DeleteConfirmationPolicies showDialog, bool permanently)
        => DeleteItemAsync(viewModel, source.FromStorageItem(), showDialog, permanently);

    #endregion

    #region Rename

    public async Task<ReturnResult> RenameAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string newName, NameCollisionOption collision, bool showExtensionDialog = true)
    {
        var returnStatus = ReturnResult.InProgress;
        var progress = new Progress<StatusCenterItemProgressModel>();
        progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

        if (!IsValidForFilename(newName))
        {
            await DialogDisplayHelper.ShowDialogAsync(
                viewModel,
                "ErrorDialogThisActionCannotBeDone".GetLocalized(),
                "ErrorDialogNameNotAllowed".GetLocalized());
            return ReturnResult.Failed;
        }

        switch (source.ItemType)
        {
            case FilesystemItemType.Directory:
                await fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
                break;

            // Prompt user when extension has changed, not when file name has changed
            case FilesystemItemType.File:
                if(showExtensionDialog && Path.GetExtension(source.Path) != Path.GetExtension(newName))
                {
                    var yesSelected = await DialogDisplayHelper.ShowDialogAsync(viewModel, "Rename".GetLocalized(), "RenameFileDialog/Text".GetLocalized(), "Yes".GetLocalized(), "No".GetLocalized());
                    if (yesSelected)
                    {
                        await fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
                        break;
                    }

                    break;
                }

                await fileSystemOperations.RenameAsync(viewModel,source, newName, collision, progress, cancellationToken);
                break;

            default:
                await fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
                break;
        }

        //await jumpListService.RemoveFolderAsync(source.Path); // Remove items from jump list

        await Task.Yield();
        return returnStatus;
    }

    public Task<ReturnResult> RenameAsync(FolderViewViewModel viewModel, IStorageItem source, string newName, NameCollisionOption collision, bool showExtensionDialog = true)
            => RenameAsync(viewModel, source.FromStorageItem(), newName, collision, showExtensionDialog);

    #endregion

    #region Paste

    public async Task<ReturnResult> PerformOperationTypeAsync(
        FolderViewViewModel viewModel,
        DataPackageOperation operation,
        DataPackageView packageView,
        string destination,
        bool showDialog,
        bool isTargetExecutable = false,
        bool isTargetPythonFile = false)
    {
        try
        {
            if (destination is null)
            {
                return default;
            }
            if (destination.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
            {
                return await RecycleItemsFromClipboard(viewModel, packageView, destination, viewModel.GetSettings().DeleteConfirmationPolicy);
            }
            else if (operation.HasFlag(DataPackageOperation.Copy))
            {
                return await CopyItemsFromClipboard(viewModel, packageView, destination, showDialog);
            }
            else if (operation.HasFlag(DataPackageOperation.Move))
            {
                return await MoveItemsFromClipboard(viewModel,packageView, destination, showDialog);
            }
            else if (operation.HasFlag(DataPackageOperation.Link))
            {
                // Open with piggybacks off of the link operation, since there isn't one for it
                if (isTargetExecutable || isTargetPythonFile)
                {
                    var items = await GetDraggedStorageItems(packageView);
                    if (isTargetPythonFile && !SoftwareHelpers.IsPythonInstalled())
                    {
                        return ReturnResult.Cancelled;
                    }

                    await NavigationHelpers.OpenItemsWithExecutableAsync(viewModel, items, destination);
                    return ReturnResult.Success;
                }
                else
                {
                    return await CreateShortcutFromClipboard(viewModel, packageView, destination, showDialog);
                }
            }
            else if (operation.HasFlag(DataPackageOperation.None))
            {
                return await CopyItemsFromClipboard(viewModel, packageView, destination, showDialog);
            }
            else
            {
                return default;
            }
        }
        finally
        {
            packageView.ReportOperationCompleted(packageView.RequestedOperation);
        }
    }

    public async Task<ReturnResult> RecycleItemsFromClipboard(FolderViewViewModel viewModel, DataPackageView packageView, string destination, DeleteConfirmationPolicies showDialog)
    {
        if (!HasDraggedStorageItems(packageView))
        {
            // Happens if you copy some text and then you Ctrl+V in Files
            return ReturnResult.BadArgumentException;
        }

        var source = await GetDraggedStorageItems(packageView);
        var returnStatus = ReturnResult.InProgress;

        source = source.Where(x => !RecycleBinHelpers.IsPathUnderRecycleBin(x.Path)); // Can't recycle items already in recyclebin
        returnStatus = await DeleteItemsAsync(viewModel, source, showDialog, false);

        return returnStatus;
    }

    public static bool HasDraggedStorageItems(DataPackageView packageView)
    {
        return packageView is not null && (packageView.Contains(StandardDataFormats.StorageItems) || packageView.Contains("FileDrop"));
    }

    public static async Task<IEnumerable<IStorageItemWithPath>> GetDraggedStorageItems(DataPackageView packageView)
    {
        var itemsList = new List<IStorageItemWithPath>();
        var hasVirtualItems = false;

        if (packageView.Contains(StandardDataFormats.StorageItems))
        {
            try
            {
                var source = await packageView.GetStorageItemsAsync();
                itemsList.AddRange(source.Select(x => x.FromStorageItem()));
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80040064 || (uint)ex.HResult == 0x8004006A)
            {
                hasVirtualItems = true;
            }
            catch (Exception)
            {
                return itemsList;
            }
        }

        // workaround for pasting folders from remote desktop (#12318)
        try
        {
            if (hasVirtualItems && packageView.Contains("FileContents"))
            {
                var descriptor = NativeClipboard.CurrentDataObject.GetData<Shell32.FILEGROUPDESCRIPTOR>("FileGroupDescriptorW");
                for (var ii = 0; ii < descriptor.cItems; ii++)
                {
                    if (descriptor.fgd[ii].dwFileAttributes.HasFlag(FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY))
                    {
                        itemsList.Add(new VirtualStorageFolder(descriptor.fgd[ii].cFileName).FromStorageItem());
                    }
                    else if (NativeClipboard.CurrentDataObject.GetData("FileContents", DVASPECT.DVASPECT_CONTENT, ii) is IStream stream)
                    {
                        var streamContent = new ComStreamWrapper(stream);
                        itemsList.Add(new VirtualStorageFile(streamContent, descriptor.fgd[ii].cFileName).FromStorageItem());
                    }
                }
            }
        }
        catch (Exception)
        {

        }

        // workaround for GetStorageItemsAsync() bug that only yields 16 items at most
        // https://learn.microsoft.com/windows/win32/shell/clipboard#cf_hdrop
        if (packageView.Contains("FileDrop"))
        {
            var fileDropData = await SafetyExtensions.IgnoreExceptions(
                () => packageView.GetDataAsync("FileDrop").AsTask());
            if (fileDropData is IRandomAccessStream stream)
            {
                stream.Seek(0);

                byte[]? dropBytes = null;
                var bytesRead = 0;
                try
                {
                    dropBytes = new byte[stream.Size];
                    bytesRead = await stream.AsStreamForRead().ReadAsync(dropBytes);
                }
                catch (COMException)
                {
                }

                if (bytesRead > 0)
                {
                    var dropStructPointer = Marshal.AllocHGlobal(dropBytes!.Length);

                    try
                    {
                        Marshal.Copy(dropBytes, 0, dropStructPointer, dropBytes.Length);
                        HDROP dropStructHandle = new(dropStructPointer);

                        var itemPaths = new List<string>();
                        var filesCount = Shell32.DragQueryFile(dropStructHandle, 0xffffffff, null, 0);
                        for (uint i = 0; i < filesCount; i++)
                        {
                            var charsNeeded = Shell32.DragQueryFile(dropStructHandle, i, null, 0);
                            var bufferSpaceRequired = charsNeeded + 1; // include space for terminating null character
                            string buffer = new('\0', (int)bufferSpaceRequired);
                            var charsCopied = Shell32.DragQueryFile(dropStructHandle, i, buffer, bufferSpaceRequired);

                            if (charsCopied > 0)
                            {
                                var path = buffer[..(int)charsCopied];
                                itemPaths.Add(Path.GetFullPath(path));
                            }
                        }

                        foreach (var path in itemPaths)
                        {
                            var isDirectory = NativeFileOperationsHelper.HasFileAttribute(path, FileAttributes.Directory);
                            itemsList.Add(StorageHelpers.FromPathAndType(path, isDirectory ? FilesystemItemType.Directory : FilesystemItemType.File));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(dropStructPointer);
                    }
                }
            }
        }

        itemsList = itemsList.DistinctBy(x => string.IsNullOrEmpty(x.Path) ? x.Item.Name : x.Path).ToList();
        return itemsList;
    }

    public async Task<ReturnResult> CopyItemsFromClipboard(FolderViewViewModel viewModel, DataPackageView packageView, string destination, bool showDialog)
    {
        var source = await GetDraggedStorageItems(packageView);

        if (!source.IsEmpty())
        {
            var returnStatus = ReturnResult.InProgress;

            var destinations = new List<string>();
            List<ShellFileItem>? binItems = null;
            foreach (var item in source)
            {
                if (RecycleBinHelpers.IsPathUnderRecycleBin(item.Path))
                {
                    binItems ??= await RecycleBinHelpers.EnumerateRecycleBin();
                    if (!binItems.IsEmpty()) // Might still be null because we're deserializing the list from Json
                    {
                        var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == item.Path); // Get original file name
                        destinations.Add(PathNormalization.Combine(destination, matchingItem?.FileName ?? item.Name));
                    }
                }
                else
                {
                    destinations.Add(PathNormalization.Combine(destination, item.Name));
                }
            }

            returnStatus = await CopyItemsAsync(viewModel, source, destinations, showDialog);

            return returnStatus;
        }

        if (packageView.Contains(StandardDataFormats.Bitmap))
        {
            try
            {
                var imgSource = await packageView.GetBitmapAsync();
                using var imageStream = await imgSource.OpenReadAsync();
                var folder = await StorageFileExtensions.DangerousGetFolderFromPathAsync(destination);
                // Set the name of the file to be the current time and date
                var file = await folder.CreateFileAsync($"{DateTime.Now:mm-dd-yy-HHmmss}.png", CreationCollisionOption.GenerateUniqueName);

                SoftwareBitmap softwareBitmap;

                // Create the decoder from the stream
                var decoder = await BitmapDecoder.CreateAsync(imageStream);

                // Get the SoftwareBitmap representation of the file
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                await BitmapHelper.SaveSoftwareBitmapToFileAsync(softwareBitmap, file, BitmapEncoder.PngEncoderId);
                return ReturnResult.Success;
            }
            catch (Exception)
            {
                return ReturnResult.UnknownException;
            }
        }

        // Happens if you copy some text and then you Ctrl+V in Files
        return ReturnResult.BadArgumentException;
    }

    #endregion

    #region Copy

    public async Task<ReturnResult> CopyItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool showDialog)
    {
        source = await source.ToListAsync();
        destination = await destination.ToListAsync();

        var returnStatus = ReturnResult.InProgress;

        /*var banner = StatusCenterHelper.AddCard_Copy(
            returnStatus,
            source,
            destination);*/
        var progress = new Progress<StatusCenterItemProgressModel>();

        /*banner.ProgressEventSource.ProgressChanged += (s, e)
            => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;*/
        progress.ProgressChanged += (s, e)
            => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

        /*var token = banner.CancellationToken;*/
        CancellationToken token = default;

        var (collisions, cancelOperation, itemsResult) = await GetCollision(viewModel, FileSystemOperationType.Copy, source, destination, showDialog);

        if (cancelOperation)
        {
            /*_statusCenterViewModel.RemoveItem(banner);*/
            return ReturnResult.Cancelled;
        }

        viewModel.ItemManipulationModel.ClearSelection();

        await fileSystemOperations.CopyItemsAsync(viewModel, (IList<IStorageItemWithPath>)source, (IList<string>)destination, collisions, progress, token);

        /*banner.Progress.ReportStatus(FileSystemStatusCode.Success);*/

        await Task.Yield();

        /*if (registerHistory && history is not null && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
        {
            foreach (var item in history.Source.Zip(history.Destination, (k, v) => new { Key = k, Value = v }).ToDictionary(k => k.Key, v => v.Value))
            {
                foreach (var item2 in itemsResult)
                {
                    if (!string.IsNullOrEmpty(item2.CustomName) && item2.SourcePath == item.Key.Path)
                    {
                        var renameHistory = await filesystemOperations.RenameAsync(item.Value, item2.CustomName, NameCollisionOption.FailIfExists, banner.ProgressEventSource, token);
                        history.Destination[history.Source.IndexOf(item.Key)] = renameHistory.Destination[0];
                    }
                }
            }
            App.HistoryWrapper.AddHistory(history);
        }

        var itemsCount = banner.TotalItemsCount;

        _statusCenterViewModel.RemoveItem(banner);

        StatusCenterHelper.AddCard_Copy(
            token.IsCancellationRequested ? ReturnResult.Cancelled : returnStatus,
            source,
            destination,
            itemsCount);*/

        return returnStatus;
    }

    #endregion

    #region Move

    public Task<ReturnResult> MoveItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool showDialog)
            => MoveItemsAsync(viewModel, source.Select((item) => item.FromStorageItem()), destination, showDialog);

    public Task<ReturnResult> MoveItemAsync(FolderViewViewModel viewModel, IStorageItem source, string destination, bool showDialog)
        => MoveItemAsync(viewModel, source.FromStorageItem(), destination, showDialog);

    public async Task<ReturnResult> MoveItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool showDialog)
    {
        source = await source.ToListAsync();
        destination = await destination.ToListAsync();

        var returnStatus = ReturnResult.InProgress;

        /*var banner = StatusCenterHelper.AddCard_Move(
            returnStatus,
            source,
            destination);*/
        var progress = new Progress<StatusCenterItemProgressModel>();

        /*banner.ProgressEventSource.ProgressChanged += (s, e)
            => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;*/
        progress.ProgressChanged += (s, e)
            => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

        /*var token = banner.CancellationToken;*/
        CancellationToken token = default;

        var (collisions, cancelOperation, itemsResult) = await GetCollision(viewModel,FileSystemOperationType.Move, source, destination, showDialog);

        if (cancelOperation)
        {
            /*_statusCenterViewModel.RemoveItem(banner);*/

            return ReturnResult.Cancelled;
        }

        /*var sw = new Stopwatch();
        sw.Start();*/

        viewModel.ItemManipulationModel.ClearSelection();

        await fileSystemOperations.MoveItemsAsync(viewModel, (IList<IStorageItemWithPath>)source, (IList<string>)destination, collisions, progress, token);

        /*banner.Progress.ReportStatus(FileSystemStatusCode.Success);*/

        await Task.Yield();

        /*if (registerHistory && history is not null && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
        {
            foreach (var item in history.Source.Zip(history.Destination, (k, v) => new { Key = k, Value = v }).ToDictionary(k => k.Key, v => v.Value))
            {
                foreach (var item2 in itemsResult)
                {
                    if (!string.IsNullOrEmpty(item2.CustomName) && item2.SourcePath == item.Key.Path)
                    {
                        var renameHistory = await filesystemOperations.RenameAsync(item.Value, item2.CustomName, NameCollisionOption.FailIfExists, banner.ProgressEventSource, token);
                        history.Destination[history.Source.IndexOf(item.Key)] = renameHistory.Destination[0];
                    }
                }
            }

            App.HistoryWrapper.AddHistory(history);
        }

        // Remove items from jump list
        source.ForEach(async x => await jumpListService.RemoveFolderAsync(x.Path));

        var itemsCount = banner.TotalItemsCount;

        _statusCenterViewModel.RemoveItem(banner);

        sw.Stop();

        StatusCenterHelper.AddCard_Move(
            token.IsCancellationRequested ? ReturnResult.Cancelled : returnStatus,
            source,
            destination,
            itemsCount);*/

        return returnStatus;
    }

    public Task<ReturnResult> MoveItemAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string destination, bool showDialog)
            => MoveItemsAsync(viewModel,source.CreateEnumerable(), destination.CreateEnumerable(), showDialog);

    public async Task<ReturnResult> MoveItemsFromClipboard(FolderViewViewModel viewModel, DataPackageView packageView, string destination, bool showDialog)
    {
        if (!HasDraggedStorageItems(packageView))
        {
            // Happens if you copy some text and then you Ctrl+V in Files
            return ReturnResult.BadArgumentException;
        }

        var source = await GetDraggedStorageItems(packageView);

        var returnStatus = ReturnResult.InProgress;

        var destinations = new List<string>();
        List<ShellFileItem>? binItems = null;
        foreach (var item in source)
        {
            if (RecycleBinHelpers.IsPathUnderRecycleBin(item.Path))
            {
                binItems ??= await RecycleBinHelpers.EnumerateRecycleBin();
                if (!binItems.IsEmpty()) // Might still be null because we're deserializing the list from Json
                {
                    var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == item.Path); // Get original file name
                    destinations.Add(PathNormalization.Combine(destination, matchingItem?.FileName ?? item.Name));
                }
            }
            else
            {
                destinations.Add(PathNormalization.Combine(destination, item.Name));
            }
        }

        returnStatus = await MoveItemsAsync(viewModel, source, destinations, showDialog);

        return returnStatus;
    }

    #endregion

    #region Create

    public async Task<ReturnResult> CreateShortcutFromClipboard(FolderViewViewModel viewModel, DataPackageView packageView, string destination, bool showDialog)
    {
        if (!HasDraggedStorageItems(packageView))
        {
            // Happens if you copy some text and then you Ctrl+V in Files
            return ReturnResult.BadArgumentException;
        }

        var source = await GetDraggedStorageItems(packageView);

        var returnStatus = ReturnResult.InProgress;
        var progress = new Progress<StatusCenterItemProgressModel>();
        progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

        source = source.Where(x => !string.IsNullOrEmpty(x.Path));
        var dest = source.Select(x => Path.Combine(destination,
            string.Format("ShortcutCreateNewSuffix".GetLocalized(), x.Name) + ".lnk"));

        source = await source.ToListAsync();
        dest = await dest.ToListAsync();

        await fileSystemOperations.CreateShortcutItemsAsync(viewModel, (IList<IStorageItemWithPath>)source, (IList<string>)dest, progress, cancellationToken);

        await Task.Yield();
        return returnStatus;
    }


    #endregion

    #region Static Methods

    // TODO: Here UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible = false.
    private static char[] RestrictedCharacters => new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

    private static readonly string[] RestrictedFileNames = new string[]
    {
            "CON", "PRN", "AUX",
            "NUL", "COM1", "COM2",
            "COM3", "COM4", "COM5",
            "COM6", "COM7", "COM8",
            "COM9", "LPT1", "LPT2",
            "LPT3", "LPT4", "LPT5",
            "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string FilterRestrictedCharacters(string input)
    {
        int invalidCharIndex;
        while ((invalidCharIndex = input.IndexOfAny(RestrictedCharacters)) >= 0)
        {
            input = input.Remove(invalidCharIndex, 1);
        }
        return input;
    }

    public static bool ContainsRestrictedCharacters(string input)
    {
        return input.IndexOfAny(RestrictedCharacters) >= 0;
    }

    public static bool IsValidForFilename(string name) => !string.IsNullOrWhiteSpace(name) && !ContainsRestrictedCharacters(name) && !ContainsRestrictedFileName(name);

    public static bool ContainsRestrictedFileName(string input)
    {
        foreach (var name in RestrictedFileNames)
        {
            if (input.StartsWith(name, StringComparison.OrdinalIgnoreCase) && (input.Length == name.Length || input[name.Length] == '.'))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<(List<FileNameConflictResolveOptionType> collisions, bool cancelOperation, IEnumerable<IFileSystemDialogConflictItemViewModel>)> GetCollision(FolderViewViewModel viewModel, FileSystemOperationType operationType, IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool forceDialog)
    {
        var incomingItems = new List<BaseFileSystemDialogItemViewModel>();
        var conflictingItems = new List<BaseFileSystemDialogItemViewModel>();
        var collisions = new Dictionary<string, FileNameConflictResolveOptionType>();

        foreach (var item in source.Zip(destination, (src, dest, index) => new { src, dest, index }))
        {
            var itemPathOrName = string.IsNullOrEmpty(item.src.Path) ? item.src.Item.Name : item.src.Path;
            incomingItems.Add(new FileSystemDialogConflictItemViewModel() { ConflictResolveOption = FileNameConflictResolveOptionType.None, SourcePath = itemPathOrName, DestinationPath = item.dest, DestinationDisplayName = Path.GetFileName(item.dest) });
            var path = incomingItems.ElementAt(item.index).SourcePath;
            if (path is not null && collisions.ContainsKey(path))
            {
                // Something strange happened, log
            }
            collisions!.AddIfNotPresent(incomingItems.ElementAt(item.index).SourcePath, FileNameConflictResolveOptionType.GenerateNewName);

            // Assume GenerateNewName when source and destination are the same
            if (string.IsNullOrEmpty(item.src.Path) || item.src.Path != item.dest)
            {
                // Same item names in both directories
                if (StorageHelpers.Exists(item.dest) ||
                    (FtpHelpers.IsFtpPath(item.dest) &&
                    await DesktopWidgets3.App.GetService<IFtpStorageService>().TryGetFileAsync(item.dest) is not null))
                {
                    (incomingItems[item.index] as FileSystemDialogConflictItemViewModel)!.ConflictResolveOption = FileNameConflictResolveOptionType.GenerateNewName;
                    conflictingItems.Add(incomingItems.ElementAt(item.index));
                }
            }
        }

        IEnumerable<IFileSystemDialogConflictItemViewModel>? itemsResult = null;

        var mustResolveConflicts = !conflictingItems.IsEmpty();
        if (mustResolveConflicts || forceDialog)
        {
            var dialogService = Ioc.Default.GetRequiredService<IDialogService>();

            var dialogViewModel = FileSystemDialogViewModel.GetDialogViewModel(
                viewModel,
                new()
                {
                    ConflictsExist = mustResolveConflicts
                },
                (false, false),
                operationType,
                incomingItems.Except(conflictingItems).ToList(), // FILESTODO: Could be optimized
                conflictingItems);

            var result = await dialogService.ShowDialogAsync(dialogViewModel);
            itemsResult = dialogViewModel.GetItemsResult();
            if (mustResolveConflicts) // If there were conflicts, result buttons are different
            {
                if (result != DialogResult.Primary) // Operation was cancelled
                {
                    return (new(), true, itemsResult);
                }
            }

            collisions.Clear();
            foreach (var item in itemsResult)
            {
                collisions!.AddIfNotPresent(item.SourcePath, item.ConflictResolveOption);
            }
        }

        // Since collisions are scrambled, we need to sort them PATH--PATH
        var newCollisions = new List<FileNameConflictResolveOptionType>();

        foreach (var src in source)
        {
            var itemPathOrName = string.IsNullOrEmpty(src.Path) ? src.Item.Name : src.Path;
            var match = collisions.SingleOrDefault(x => x.Key == itemPathOrName);
            var fileNameConflictResolveOptionType = (match.Key is not null) ? match.Value : FileNameConflictResolveOptionType.Skip;
            newCollisions.Add(fileNameConflictResolveOptionType);
        }

        return (newCollisions, false, itemsResult ?? new List<IFileSystemDialogConflictItemViewModel>());
    }


    #endregion

    public void Dispose()
    {
        fileSystemOperations?.Dispose();
        fileSystemOperations = null!;
    }
}
