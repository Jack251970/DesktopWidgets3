// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Enums;

namespace Files.App.Helpers;

/// <summary>
/// Represents item for a folder's layout preferences.
/// </summary>
public class LayoutPreferencesItem
{
    public bool IsAdaptiveLayoutOverridden;

    public FolderLayoutModes LayoutMode;

    // Constructor

    public LayoutPreferencesItem()
    {
        // TODO: Add UserSettingsService.FoldersSettingsService.DefaultLayoutMode;
        var defaultLayout = FolderLayoutModes.DetailsView;

        LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;
    }
}