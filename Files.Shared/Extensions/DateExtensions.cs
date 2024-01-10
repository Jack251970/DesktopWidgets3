// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Runtime.InteropServices.ComTypes;

namespace Files.Shared.Extensions;

public static class DateExtensions
{
	public static DateTime ToDateTime(this FILETIME time)
	{
		var low = (uint)time.dwLowDateTime;
		var high = (ulong)time.dwHighDateTime;
		var fileTime = (long)((high << 32) + low);

		try
		{
			return DateTime.FromFileTimeUtc(fileTime);
		}
		catch
		{
			return DateTime.FromFileTimeUtc(0xFFFFFFFF);
		}
	}
}
