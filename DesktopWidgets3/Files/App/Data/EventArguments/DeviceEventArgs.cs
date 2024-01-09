// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace DesktopWidgets3.Files.App.Data.EventArguments;

public class DeviceEventArgs : EventArgs
{
	public string DeviceName { get; }
	public string DeviceId { get; }

	public DeviceEventArgs(string deviceName, string deviceId)
	{
		DeviceName = deviceName;
		DeviceId = deviceId;
	}
}
