// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Parameters;

public sealed class NavigationParams
{
    // CHECK: Required is just for final checking.
    public /*required*/ IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    public string? NavPath { get; set; }

	public string? SelectItem { get; set; }
}
