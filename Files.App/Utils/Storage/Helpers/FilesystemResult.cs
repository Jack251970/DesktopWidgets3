// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Win32.Foundation;

namespace Files.App.Utils.Storage;

public class FilesystemResult(FileSystemStatusCode errorCode)
{
    public FileSystemStatusCode ErrorCode { get; } = errorCode;

    public static implicit operator FileSystemStatusCode(FilesystemResult res) => res.ErrorCode;
    public static implicit operator FilesystemResult(FileSystemStatusCode res) => new(res);

    public static implicit operator bool(FilesystemResult res) => res?.ErrorCode is FileSystemStatusCode.Success;
    public static explicit operator FilesystemResult(bool res) => new(res ? FileSystemStatusCode.Success : FileSystemStatusCode.Generic);


    public static implicit operator BOOL(FilesystemResult res) => res?.ErrorCode is FileSystemStatusCode.Success;
    public static explicit operator FilesystemResult(BOOL res) => new(res ? FileSystemStatusCode.Success : FileSystemStatusCode.Generic);
}

public sealed class FilesystemResult<T>(T result, FileSystemStatusCode errorCode) : FilesystemResult(errorCode)
{
    public T Result { get; } = result;

    public static implicit operator T(FilesystemResult<T> res) => res.Result;
}