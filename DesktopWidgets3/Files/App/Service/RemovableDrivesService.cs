// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
using Files.App.Extensions;
using Files.App.Storage.WindowsStorage;
using Files.App.Utils;
using Files.App.Utils.Storage;
using Files.Core.Data.Enums;
using Files.Core.Data.Models;
using Files.Core.Services;
using Files.Core.Storage.LocatableStorage;
using Windows.Storage;

namespace Files.App.Services;

public class RemovableDrivesService : IRemovableDrivesService
{
    public IStorageDeviceWatcher CreateWatcher()
    {
        return new WindowsStorageDeviceWatcher();
    }

    public async IAsyncEnumerable<ILocatableFolder> GetDrivesAsync()
    {
        var list = DriveInfo.GetDrives();
        var googleDrivePath = DesktopWidgets3.App.AppModel.GoogleDrivePath;
        var pCloudDrivePath = DesktopWidgets3.App.AppModel.PCloudDrivePath;

        foreach (var drive in list)
        {
            var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Name).AsTask());
            if (res.ErrorCode is FileSystemStatusCode.Unauthorized)
            {
                continue;
            }
            else if (!res)
            {
                continue;
            }

            using var thumbnail = await DriveHelpers.GetThumbnailAsync(res.Result);
            var type = DriveHelpers.GetDriveType(drive);
            var label = DriveHelpers.GetExtendedDriveLabel(drive);
            var driveItem = await DriveItem.CreateFromPropertiesAsync(res.Result, drive.Name.TrimEnd('\\'), label, type, thumbnail);

            // Don't add here because Google Drive is already displayed under cloud drives
            if (drive.Name == googleDrivePath || drive.Name == pCloudDrivePath)
            {
                continue;
            }

            yield return driveItem;
        }
    }

    public async Task<ILocatableFolder> GetPrimaryDriveAsync()
    {
        var cDrivePath = @"C:\";
        return new WindowsStorageFolder(await StorageFolder.GetFolderFromPathAsync(cDrivePath));
    }

    public async Task UpdateDrivePropertiesAsync(ILocatableFolder drive)
    {
        var rootModified = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Path).AsTask());
        if (rootModified && drive is DriveItem matchingDriveEjected)
        {
            _ = DesktopWidgets3.App.DispatcherQueue.EnqueueOrInvokeAsync(() =>
            {
                matchingDriveEjected.Root = rootModified.Result;
                matchingDriveEjected.Text = rootModified.Result.DisplayName;
                return matchingDriveEjected.UpdatePropertiesAsync();
            });
        }
    }
}
