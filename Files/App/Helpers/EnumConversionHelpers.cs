// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;

namespace Files.App.Helpers;

public static class EnumConversionHelpers
{
    public static CreationCollisionOption Convert(this NameCollisionOption option)
    {
        return option switch
        {
            NameCollisionOption.FailIfExists => CreationCollisionOption.FailIfExists,
            NameCollisionOption.GenerateUniqueName => CreationCollisionOption.GenerateUniqueName,
            NameCollisionOption.ReplaceExisting => CreationCollisionOption.ReplaceExisting,
            _ => CreationCollisionOption.GenerateUniqueName,
        };
    }
}
