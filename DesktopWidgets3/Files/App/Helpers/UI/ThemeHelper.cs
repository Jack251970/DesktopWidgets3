// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Helpers;

public static class ThemeHelper
{
    /// <summary>
    /// Gets or sets the RequestedTheme of the root element.
    /// </summary>
    public static ElementTheme RootTheme
    {
        get; set;
    } = ElementTheme.Default;
}