// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Parameters;

public class NavigationParams
{
    // CHECK: Required is just for checking.
    public /*required*/ IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    public string? NavPath { get; set; }

	public string? SelectItem { get; set; }
}
