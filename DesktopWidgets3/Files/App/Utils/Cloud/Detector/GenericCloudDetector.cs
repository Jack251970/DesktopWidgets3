// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.Core.Utils.Cloud;

namespace DesktopWidgets3.Files.App.Utils.Cloud;

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
