// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Parameters;

public sealed class ColumnParam : NavigationArguments
{
    public int Column { get; set; }

    public ListView ListView { get; set; } = null!;

	public ColumnLayoutPage? Source { get; set; }
}
