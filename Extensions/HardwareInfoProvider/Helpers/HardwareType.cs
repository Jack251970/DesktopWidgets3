// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HardwareInfoProvider.Helpers;

public enum HardwareType
{
    /// <summary>
    /// CPU related data.
    /// </summary>
    CPU,

    /// <summary>
    /// Memory related data.
    /// </summary>
    Memory,

    /// <summary>
    /// GPU related data.
    /// </summary>
    GPU,

    /// <summary>
    /// Network related data.
    /// </summary>
    Network,

    /// <summary>
    /// Disk related data.
    /// </summary>
    Disk
}
