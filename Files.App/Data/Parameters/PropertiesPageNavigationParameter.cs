// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Parameters;

public sealed class PropertiesPageNavigationParameter
{
    // CHECK: Required is just for final checking.
    public /*required*/ IFolderViewViewModel FolderViewViewModel = null!;

	public CancellationTokenSource CancellationTokenSource = null!;

	public object Parameter = null!;

    public IShellPage AppInstance = null!;

    public Window Window = null!;
}
