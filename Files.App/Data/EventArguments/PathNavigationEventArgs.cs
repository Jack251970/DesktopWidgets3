// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments;

public sealed class PathNavigationEventArgs
{
	public string ItemPath { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public bool IsFile { get; set; }
}
