// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage;

public static class ErrorCodeConverter
{
	public static ReturnResult ToStatus(this FileSystemStatusCode errorCode)
	{
        return errorCode switch
        {
            FileSystemStatusCode.Success => ReturnResult.Success,
            FileSystemStatusCode.Unauthorized or FileSystemStatusCode.InUse => ReturnResult.AccessUnauthorized,
            FileSystemStatusCode.NotFound => ReturnResult.IntegrityCheckFailed,
            FileSystemStatusCode.NotAFolder or FileSystemStatusCode.NotAFile => ReturnResult.BadArgumentException,
            FileSystemStatusCode.InProgress => ReturnResult.InProgress,
            _ => ReturnResult.Failed,
        };
    }
}