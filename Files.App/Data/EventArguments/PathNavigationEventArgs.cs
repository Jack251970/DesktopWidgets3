// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments;

public class PathNavigationEventArgs
{
	public string ItemPath { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public bool IsFile { get; set; }
}
