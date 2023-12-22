// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Utils.Cloud;

namespace Files.App.Utils.Cloud;

/// <summary>
/// Provides an utility for generic cloud detection.
/// </summary>
public class GenericCloudDetector : AbstractCloudDetector
{
    protected async override IAsyncEnumerable<ICloudProvider> GetProviders()
    {
        foreach (var provider in await CloudDrivesDetector.DetectCloudDrives())
        {
            yield return provider!;
        }
    }
}
