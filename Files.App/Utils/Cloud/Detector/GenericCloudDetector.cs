// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Cloud;

/// <summary>
/// Provides an utility for generic cloud detection.
/// </summary>
public sealed class GenericCloudDetector : AbstractCloudDetector
{
	protected async override IAsyncEnumerable<ICloudProvider> GetProviders()
	{
		foreach (var provider in await CloudDrivesDetector.DetectCloudDrives())
		{
			yield return provider;
		}
	}
}
