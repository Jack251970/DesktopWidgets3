// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Extensions;
using FluentFTP;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage;

internal static class FtpHelpers
{
	public static string GetFtpPath(string path)
	{
		path = path.Replace("\\", "/", StringComparison.Ordinal);

		var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
		var hostIndex = path.IndexOf('/', schemaIndex);

		return hostIndex == -1 ? "/" : path[hostIndex..];
	}

	public static Task EnsureConnectedAsync(this AsyncFtpClient ftpClient, CancellationToken cancellationToken = default)
	{
		return ftpClient.IsConnected ? Task.CompletedTask : ftpClient.Connect(cancellationToken);
	}

	public static string GetFtpHost(string path)
	{
		var authority = GetFtpAuthority(path);
		var index = authority.IndexOf(':', StringComparison.Ordinal);

		return index == -1 ? authority : authority[..index];
	}

	public static ushort GetFtpPort(string path)
	{
		var authority = GetFtpAuthority(path);
		var index = authority.IndexOf(':', StringComparison.Ordinal);

		if (index != -1)
        {
            return ushort.Parse(authority[(index + 1)..]);
        }

        return path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase) ? (ushort)990 : (ushort)21;
	}

	public static string GetFtpAuthority(string path)
	{
		path = path.Replace("\\", "/", StringComparison.Ordinal);
		var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
		var hostIndex = path.IndexOf('/', schemaIndex);

		if (hostIndex == -1)
        {
            hostIndex = path.Length;
        }

        return path[schemaIndex..hostIndex];
	}

	public static AsyncFtpClient GetFtpClient(string ftpPath)
	{
		var host = GetFtpHost(ftpPath);
		var port = GetFtpPort(ftpPath);
		var credentials = FtpManager.Credentials.Get(host, FtpManager.Anonymous);

		return new(host, credentials, port);
	}
}
