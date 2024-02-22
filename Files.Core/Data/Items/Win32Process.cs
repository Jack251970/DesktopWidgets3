// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Items;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

public class Win32Process
{
	public string Name { get; set; }

	public int Pid { get; set; }

	public string FileName { get; set; }

	public string AppName { get; set; }
}
