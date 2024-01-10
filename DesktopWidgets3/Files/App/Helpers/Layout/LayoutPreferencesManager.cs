// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Data.EventArguments;
using Files.App.Helpers;
using Files.Core.Data.Enums;

namespace Files.App.Data.Models;

/// <summary>
/// Represents manager for layout preferences settings.
/// </summary>
public class LayoutPreferencesManager : ObservableObject
{
    private readonly FolderLayoutModes? _rootLayoutMode;

    public FolderLayoutModes LayoutMode
    {
        get => _rootLayoutMode ?? LayoutPreferencesItem.LayoutMode;
        set
        {
            if (SetProperty(ref LayoutPreferencesItem.LayoutMode, value, nameof(LayoutMode)))
            {
                LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
            }
        }
    }

    private LayoutPreferencesItem? _LayoutPreferencesItem;
    public LayoutPreferencesItem LayoutPreferencesItem
    {
        get => _LayoutPreferencesItem!;
        private set
        {
            if (SetProperty(ref _LayoutPreferencesItem, value))
            {
                OnPropertyChanged(nameof(LayoutMode));
                /*OnPropertyChanged(nameof(GridViewSize));
                OnPropertyChanged(nameof(GridViewSizeKind));
                OnPropertyChanged(nameof(IsAdaptiveLayoutEnabled));
                OnPropertyChanged(nameof(DirectoryGroupOption));
                OnPropertyChanged(nameof(DirectorySortOption));
                OnPropertyChanged(nameof(DirectorySortDirection));
                OnPropertyChanged(nameof(DirectoryGroupDirection));
                OnPropertyChanged(nameof(DirectoryGroupByDateUnit));
                OnPropertyChanged(nameof(SortDirectoriesAlongsideFiles));
                OnPropertyChanged(nameof(ColumnsViewModel));*/
            }
        }
    }

    #region Events

    public event EventHandler<LayoutPreferenceEventArgs>? LayoutPreferencesUpdateRequired;

    #endregion

    #region Constructors

    public LayoutPreferencesManager()
    {
        LayoutPreferencesItem = new LayoutPreferencesItem();
    }

    public LayoutPreferencesManager(FolderLayoutModes modeOverride) : this()
    {
        _rootLayoutMode = modeOverride;
        LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
    }

    #endregion

    #region Methods

    public uint GetIconSize()
    {
        return LayoutMode switch
        {
            FolderLayoutModes.DetailsView
                => Constants.Browser.DetailsLayoutBrowser.DetailsViewSize,
            FolderLayoutModes.ColumnView
                => Constants.Browser.ColumnViewBrowser.ColumnViewSize,
            FolderLayoutModes.TilesView
                => Constants.Browser.GridViewBrowser.GridViewSizeSmall,
            /*_ when GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeSmall
                => Constants.Browser.GridViewBrowser.GridViewSizeSmall,
            _ when GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeMedium
                => Constants.Browser.GridViewBrowser.GridViewSizeMedium,
            _ when GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeLarge
                => Constants.Browser.GridViewBrowser.GridViewSizeLarge,*/
            _ => Constants.Browser.GridViewBrowser.GridViewSizeMax,
        };
    }

    #endregion
}