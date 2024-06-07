// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items;

public sealed class IconFileInfo(byte[] iconData, int index)
{
    public byte[] IconData { get; } = iconData;

    public int Index { get; } = index;
}
