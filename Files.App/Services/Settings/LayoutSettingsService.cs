// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.AppCenter.Analytics;

namespace Files.App.Services.Settings;

internal sealed class LayoutSettingsService : BaseObservableJsonSettings, ILayoutSettingsService
{
	/*public LayoutSettingsService(ISettingsSharingContext settingsSharingContext)
	{
		// Register root
		RegisterSettingsContext(settingsSharingContext);
	}*/

    public void Initialize(IUserSettingsService userSettingsService)
    {
        // Register root
        var settingsSharingContext = ((UserSettingsService)userSettingsService).GetSharingContext();
        RegisterSettingsContext(settingsSharingContext);
    }

    public bool SyncFolderPreferencesAcrossDirectories
    {
        get => Get(false);
        set => Set(value);
    }

    public FolderLayoutModes DefaultLayoutMode
    {
        get => (FolderLayoutModes)Get((long)FolderLayoutModes.Adaptive);
        set => Set((long)value);
    }

    public SortOption DefaultSortOption
    {
        get => (SortOption)Get((long)SortOption.Name);
        set => Set((long)value);
    }

    public SortDirection DefaultDirectorySortDirection
    {
        get => (SortDirection)Get((long)SortDirection.Ascending);
        set => Set((long)value);
    }

    public bool DefaultSortDirectoriesAlongsideFiles
    {
        get => Get(false);
        set => Set(value);
    }

    public bool DefaultSortFilesFirst
    {
        get => Get(false);
        set => Set(value);
    }

    public GroupOption DefaultGroupOption
    {
        get => (GroupOption)Get((long)GroupOption.None);
        set => Set((long)value);
    }

    public SortDirection DefaultDirectoryGroupDirection
    {
        get => (SortDirection)Get((long)SortDirection.Ascending);
        set => Set((long)value);
    }

    public GroupByDateUnit DefaultGroupByDateUnit
    {
        get => (GroupByDateUnit)Get((long)GroupByDateUnit.Year);
        set => Set((long)value);
    }

    // CHANGE: Change to new default setting.
    public double GitStatusColumnWidth
    {
        get => Get(67d);
        set
        {
            if (ShowGitStatusColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double GitLastCommitDateColumnWidth
    {
        get => Get(118d);
        set
        {
            if (ShowGitLastCommitDateColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double GitLastCommitMessageColumnWidth
    {
        get => Get(118d);
        set
        {
            if (ShowGitLastCommitMessageColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double GitCommitAuthorColumnWidth
    {
        get => Get(118d);
        set
        {
            if (ShowGitCommitAuthorColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double GitLastCommitShaColumnWidth
    {
        get => Get(67d);
        set
        {
            if (ShowGitLastCommitShaColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double TagColumnWidth
    {
        get => Get(118d);
        set
        {
            if (ShowFileTagColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double NameColumnWidth
    {
        get => Get(201d);
        set => Set(value);
    }

    // CHANGE: Change to new default setting.
    public double DateModifiedColumnWidth
    {
        get => Get(168d);
        set
        {
            if (ShowDateColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double TypeColumnWidth
    {
        get => Get(118d);
        set
        {
            if (ShowTypeColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double DateCreatedColumnWidth
    {
        get => Get(168d);
        set
        {
            if (ShowDateCreatedColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double SizeColumnWidth
    {
        get => Get(84d);
        set
        {
            if (ShowSizeColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double DateDeletedColumnWidth
    {
        get => Get(168d);
        set
        {
            if (ShowDateDeletedColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double PathColumnWidth
    {
        get => Get(168d);
        set
        {
            if (ShowPathColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double OriginalPathColumnWidth
    {
        get => Get(168d);
        set
        {
            if (ShowOriginalPathColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public double SyncStatusColumnWidth
    {
        get => Get(42d);
        set
        {
            if (ShowSyncStatusColumn)
            {
                Set(value);
            }
        }
    }

    // CHANGE: Change to new default setting.
    public bool ShowDateColumn
    {
        get => Get(false);
        set => Set(value);
    }

    public bool ShowDateCreatedColumn
    {
        get => Get(false);
        set => Set(value);
    }

    public bool ShowTypeColumn
    {
        get => Get(true);
        set => Set(value);
    }

    public bool ShowSizeColumn
    {
        get => Get(true);
        set => Set(value);
    }

    public bool ShowGitStatusColumn
    {
        get => Get(false);
        set => Set(value);
    }

    public bool ShowGitLastCommitDateColumn
    {
        get => Get(false);
        set => Set(value);
    }

    public bool ShowGitLastCommitMessageColumn
    {
        get => Get(false);
        set => Set(value);
    }

    public bool ShowGitCommitAuthorColumn
    {
        get => Get(false);
        set => Set(value);
    }

    public bool ShowGitLastCommitShaColumn
    {
        get => Get(false);
        set => Set(value);
    }

    // CHANGE: Change to new default setting.
    public bool ShowFileTagColumn
    {
        get => Get(false);
        set => Set(value);
    }

    // CHANGE: Change to new default setting.
    public bool ShowDateDeletedColumn
    {
        get => Get(false);
        set => Set(value);
    }

    // CHANGE: Change to new default setting.
    public bool ShowPathColumn
    {
        get => Get(false);
        set => Set(value);
    }

    // CHANGE: Change to new default setting.
    public bool ShowOriginalPathColumn
    {
        get => Get(false);
        set => Set(value);
    }

    public bool ShowSyncStatusColumn
    {
        get => Get(true);
        set => Set(value);
    }

    public DetailsViewSizeKind DetailsViewSize
    {
        get => Get(DetailsViewSizeKind.Small);
        set => Set(value);
    }

    public ListViewSizeKind ListViewSize
    {
        get => Get(ListViewSizeKind.Small);
        set => Set(value);
    }

    public TilesViewSizeKind TilesViewSize
    {
        get => Get(TilesViewSizeKind.Small);
        set => Set(value);
    }

    // CHANGE: Change to new default setting.
    public GridViewSizeKind GridViewSize
    {
        get => Get(GridViewSizeKind.Small);
        set => Set(value);
    }

    public ColumnsViewSizeKind ColumnsViewSize
    {
        get => Get(ColumnsViewSizeKind.Small);
        set => Set(value);
    }

    protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
    {
        switch (e.SettingName)
        {
            case nameof(SyncFolderPreferencesAcrossDirectories):
            case nameof(DefaultLayoutMode):
            case nameof(DefaultSortOption):
            case nameof(DefaultDirectorySortDirection):
            case nameof(DefaultSortDirectoriesAlongsideFiles):
            case nameof(DefaultSortFilesFirst):
            case nameof(DefaultGroupOption):
            case nameof(DefaultDirectoryGroupDirection):
            case nameof(DefaultGroupByDateUnit):
            case nameof(GitStatusColumnWidth):
            case nameof(GitLastCommitDateColumnWidth):
            case nameof(GitLastCommitMessageColumnWidth):
            case nameof(GitCommitAuthorColumnWidth):
            case nameof(GitLastCommitShaColumnWidth):
            case nameof(TagColumnWidth):
            case nameof(NameColumnWidth):
            case nameof(DateModifiedColumnWidth):
            case nameof(TypeColumnWidth):
            case nameof(DateCreatedColumnWidth):
            case nameof(SizeColumnWidth):
            case nameof(ShowDateColumn):
            case nameof(ShowDateCreatedColumn):
            case nameof(ShowTypeColumn):
            case nameof(ShowSizeColumn):
            case nameof(ShowGitStatusColumn):
            case nameof(ShowGitLastCommitDateColumn):
            case nameof(ShowGitLastCommitMessageColumn):
            case nameof(ShowGitCommitAuthorColumn):
            case nameof(ShowGitLastCommitShaColumn):
            case nameof(ShowFileTagColumn):
                Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
                break;
        }

        base.RaiseOnSettingChangedEvent(sender, e);
    }
}
