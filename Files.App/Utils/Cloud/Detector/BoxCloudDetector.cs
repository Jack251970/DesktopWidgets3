// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Cloud;

/// <summary>
/// Provides an utility for Box Cloud detection.
/// </summary>
public class BoxCloudDetector : AbstractCloudDetector
{
	protected async override IAsyncEnumerable<ICloudProvider> GetProviders()
	{
		var configPathBoxDrive = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Box\Box\data\shell\sync_root_folder.txt");

		var configPathBoxSync = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Box Sync\sync_root_folder.txt");

		StorageFile configFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(configPathBoxDrive).AsTask());

		configFile ??= await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(configPathBoxSync).AsTask());

		if (configFile is not null)
		{
			var syncPath = await FileIO.ReadTextAsync(configFile);

			if (!string.IsNullOrEmpty(syncPath))
			{
				yield return new CloudProvider(CloudProviders.Box)
				{
					Name = "Box",
					SyncFolder = syncPath,
				};
			}
		}
	}
}
