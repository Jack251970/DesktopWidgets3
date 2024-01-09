// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.Core.Data.Enums;
using Windows.Storage;

namespace DesktopWidgets3.Files.App.Utils.Storage;

public interface IStorageItemWithPath
{
    public string Name { get; }

    public string Path { get; }

    public IStorageItem Item { get; }

    public FilesystemItemType ItemType { get; }
}
