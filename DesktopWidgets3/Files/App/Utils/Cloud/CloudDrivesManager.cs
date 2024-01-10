// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.Core.Utils.Cloud;
using System.Collections.Specialized;
using Windows.Storage;
using DriveType = Files.App.Data.Items.DriveType;

namespace Files.App.Utils.Cloud;

public static class CloudDrivesManager
{
    private static ICloudDetector _detector = null!;

    public static EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

    private static readonly List<DriveItem> _Drives = new();
    public static IReadOnlyList<DriveItem> Drives
    {
        get
        {
            lock (_Drives)
            {
                return _Drives.ToList().AsReadOnly();
            }
        }
    }

    public static async Task UpdateDrivesAsync()
    {
        _detector ??= new CloudDetector();

        var providers = await _detector.DetectCloudProvidersAsync();
        if (providers is null)
        {
            return;
        }

        foreach (var provider in providers)
        {
            var cloudProviderItem = new DriveItem()
            {
                Text = provider.Name,
                Path = provider.SyncFolder,
                Type = DriveType.CloudDrive,
            };

            try
            {
                cloudProviderItem.Root = await StorageFolder.GetFolderFromPathAsync(cloudProviderItem.Path);

                _ = DesktopWidgets3.App.DispatcherQueue.EnqueueOrInvokeAsync(() => cloudProviderItem.UpdatePropertiesAsync());
            }
            catch (Exception)
            {
                
            }

            /*cloudProviderItem.MenuOptions = new ContextMenuOptions()
            {
                IsLocationItem = true,
                ShowEjectDevice = cloudProviderItem.IsRemovable,
                ShowShellItems = true,
                ShowProperties = true,
            };*/

            var iconData = provider.IconData ?? await FileThumbnailHelper.LoadIconWithoutOverlayAsync(provider.SyncFolder, 24);
            if (iconData is not null)
            {
                cloudProviderItem.IconData = iconData;

                await DesktopWidgets3.App.DispatcherQueue.EnqueueOrInvokeAsync(async ()
                    => cloudProviderItem.Icon = (await iconData.ToBitmapAsync())!);
            }

            lock (_Drives)
            {
                if (_Drives.Any(x => x.Path == cloudProviderItem.Path))
                {
                    continue;
                }

                _Drives.Add(cloudProviderItem);
            }

            /*// Handle data changed event to side bar
            DataChanged?.Invoke(
                SectionType.CloudDrives,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, cloudProviderItem)
            );*/
        }
    }
}
