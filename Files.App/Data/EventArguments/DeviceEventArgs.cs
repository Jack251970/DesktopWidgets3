// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments;

public sealed class DeviceEventArgs(string deviceName, string deviceId) : EventArgs
{
    public string DeviceName { get; } = deviceName;
    public string DeviceId { get; } = deviceId;
}
