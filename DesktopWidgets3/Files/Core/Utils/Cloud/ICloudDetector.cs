﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace DesktopWidgets3.Files.Core.Utils.Cloud;

public interface ICloudDetector
{
    Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync();
}