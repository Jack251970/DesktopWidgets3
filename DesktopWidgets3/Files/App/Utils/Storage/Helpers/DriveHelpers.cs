// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Helpers;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;

namespace Files.App.Utils.Storage;

public static class DriveHelpers
{
    public static async Task<StorageFolderWithPath> GetRootFromPathAsync(string devicePath)
    {
        if (!Path.IsPathRooted(devicePath))
        {
            return null!;
        }

        // Handle if this is USB device or network share
        /*var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

        var rootPath = Path.GetPathRoot(devicePath);
        // USB device
        if (devicePath.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            // Check among already discovered drives
            StorageFolder matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
                PathNormalization.NormalizePath(x.Path) == PathNormalization.NormalizePath(rootPath!))?.Root;
            if (matchingDrive is null)
            {
                // Check on all removable drives
                var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
                var normalizedRootPath = PathNormalization.NormalizePath(rootPath!).Replace(@"\\?\", string.Empty, StringComparison.Ordinal);
                foreach (var item in remDevices)
                {
                    try
                    {
                        var root = StorageDevice.FromId(item.Id);
                        if (normalizedRootPath == root.Name.ToUpperInvariant())
                        {
                            matchingDrive = root;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore this..
                    }
                }
            }
            if (matchingDrive is not null)
            {
                return new StorageFolderWithPath(matchingDrive, rootPath!);
            }
        }
        // Network share
        else if (devicePath.StartsWith(@"\\", StringComparison.Ordinal) &&
            !devicePath.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
        {
            var lastSepIndex = rootPath!.LastIndexOf(@"\", StringComparison.Ordinal);
            rootPath = lastSepIndex > 1 ? rootPath.Substring(0, lastSepIndex) : rootPath; // Remove share name
            return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(rootPath), rootPath);
        }*/
        await Task.CompletedTask;
        
        // It's ok to return null here, on normal drives StorageFolder.GetFolderFromPathAsync works
        return null!;
    }
}