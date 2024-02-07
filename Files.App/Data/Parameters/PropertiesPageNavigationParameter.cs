// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace Files.App.Data.Parameters;

public class PropertiesPageNavigationParameter
{
    // CHECK: Required is just for checking.
    public /*required*/ IFolderViewViewModel FolderViewViewModel = null!;

	public CancellationTokenSource CancellationTokenSource = null!;

	public object Parameter = null!;

    public IShellPage AppInstance = null!;

    public Window Window = null!;

    public AppWindow AppWindow = null!;
}
