// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Foundation;
using Windows.Storage;

namespace DesktopWidgets3.Files.App.Utils.Storage;

public interface ICreateFileWithStream
{
    IAsyncOperation<BaseStorageFile> CreateFileAsync(Stream contents, string desiredName);

    IAsyncOperation<BaseStorageFile> CreateFileAsync(Stream contents, string desiredName, CreationCollisionOption options);
}
