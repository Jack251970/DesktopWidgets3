// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using FluentFTP.Exceptions;
using SevenZip;

namespace Files.App.Utils.Storage;

public interface IPasswordProtectedItem
{
    IFolderViewViewModel FolderViewViewModel { get; set; }

	StorageCredential Credentials { get; set; }

	Func<IFolderViewViewModel, IPasswordProtectedItem, Task<StorageCredential>> PasswordRequestedCallback { get; set; }

	async Task<TOut> RetryWithCredentialsAsync<TOut>(Func<Task<TOut>> func, Exception exception)
	{
		var handled = exception is SevenZipOpenFailedException szofex && szofex.Result is OperationResult.WrongPassword ||
			exception is ExtractionFailedException efex && efex.Result is OperationResult.WrongPassword ||
			exception is FtpAuthenticationException;

		if (!handled || PasswordRequestedCallback is null)
        {
            throw exception;
        }

        Credentials = await PasswordRequestedCallback(FolderViewViewModel, this);

		return await func();
	}

	async Task RetryWithCredentialsAsync(Func<Task> func, Exception exception)
	{
		var handled = exception is SevenZipOpenFailedException szofex && szofex.Result is OperationResult.WrongPassword ||
			exception is ExtractionFailedException efex && efex.Result is OperationResult.WrongPassword ||
			exception is FtpAuthenticationException;

		if (!handled || PasswordRequestedCallback is null)
        {
            throw exception;
        }

        Credentials = await PasswordRequestedCallback(FolderViewViewModel,this);

		await func();
	}

	void CopyFrom(IPasswordProtectedItem parent)
	{
		Credentials = parent.Credentials;
		PasswordRequestedCallback = parent.PasswordRequestedCallback;
	}
}
