// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;
using static Files.App.Utils.Shell.Win32API;

namespace Files.App.Helpers;

public static class UIHelpers
{
    private static readonly IconFileInfo ShieldIconResource = LoadShieldIconResource();

    public static async Task<BitmapImage?> GetShieldIconResource()
    {
        return ShieldIconResource is not null
            ? await ShieldIconResource.IconData.ToBitmapAsync()
            : null;
    }

    private static IconFileInfo LoadShieldIconResource()
    {
        var imageres = Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");
        var imageResList = ExtractSelectedIconsFromDLL(imageres, 
            new List<int>() { Constants.ImageRes.ShieldIcon }, 16);

        return imageResList.First();
    }
}