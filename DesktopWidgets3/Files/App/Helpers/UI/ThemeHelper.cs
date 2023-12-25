// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using DesktopWidgets3.ViewModels.Pages;
using DesktopWidgets3.Views.Windows;
using Files.App.Extensions;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Vanara.Extensions;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;

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