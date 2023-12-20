// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;

namespace Files.App.Helpers;

public class NativeWinApiHelper
{
    public static Task<string?> GetFileAssociationAsync(string filePath)
            => Win32API.GetFileAssociationAsync(filePath, true);
}
