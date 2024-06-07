// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace Files.App.Utils.Cloud;

/// <summary>
/// Provides an utility for Drop Box Cloud detection.
/// </summary>
public sealed class DropBoxCloudDetector : AbstractCloudDetector
{
	protected async override IAsyncEnumerable<ICloudProvider> GetProviders()
	{
		var jsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Dropbox\info.json");

		var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
		using var jsonDoc = JsonDocument.Parse(await FileIO.ReadTextAsync(configFile));
		var jsonElem = jsonDoc.RootElement;

		if (jsonElem.TryGetProperty("personal", out var inner))
		{
			var dropBoxPath = inner.GetProperty("path").GetString()!;

			yield return new CloudProvider(CloudProviders.DropBox)
			{
				Name = "Dropbox",
				SyncFolder = dropBoxPath,
			};
		}

		if (jsonElem.TryGetProperty("business", out var innerBusiness))
		{
			var dropBoxPath = innerBusiness.GetProperty("path").GetString();

			yield return new CloudProvider(CloudProviders.DropBox)
			{
				Name = "Dropbox Business",
				SyncFolder = dropBoxPath!,
			};
		}
	}
}
