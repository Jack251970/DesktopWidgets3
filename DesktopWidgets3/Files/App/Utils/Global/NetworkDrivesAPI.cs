// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using static Vanara.PInvoke.Mpr;

namespace Files.App.Utils;

public class NetworkDrivesAPI
{
    public static bool DisconnectNetworkDrive(string drive)
    {
        return WNetCancelConnection2(drive.TrimEnd('\\'), CONNECT.CONNECT_UPDATE_PROFILE, true).Succeeded;
    }
}
