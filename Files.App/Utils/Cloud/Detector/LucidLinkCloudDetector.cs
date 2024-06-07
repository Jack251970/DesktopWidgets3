// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Cloud;

/// <summary>
/// Provides an utility for LucidLink Cloud detection.
/// </summary>
public sealed class LucidLinkCloudDetector : AbstractCloudDetector
{
	protected async override IAsyncEnumerable<ICloudProvider> GetProviders()
	{
		var jsonPath = Path.Combine(Environment.GetEnvironmentVariable("UserProfile")!, ".lucid", "app.json");

		var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
		using var jsonFile = JsonDocument.Parse(await FileIO.ReadTextAsync(configFile));
		var jsonElem = jsonFile.RootElement;

		if (jsonElem.TryGetProperty("filespaces", out var filespaces))
		{
			foreach (var inner in filespaces.EnumerateArray())
			{
				var syncFolder = inner.GetProperty("filespaceName").GetString();

				var orgNameFilespaceName = syncFolder!.Split(".");
				var path = Path.Combine(@"C:\Volumes", orgNameFilespaceName[1], orgNameFilespaceName[0]);
				var filespaceName = orgNameFilespaceName[0];

				var iconPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles")!, "Lucid", "resources", "Logo.ico");
				StorageFile iconFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(iconPath).AsTask());

				yield return new CloudProvider(CloudProviders.LucidLink)
				{
					Name = $"Lucid Link ({filespaceName})",
					SyncFolder = path,
					IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
				};
			}
		}
	}
}