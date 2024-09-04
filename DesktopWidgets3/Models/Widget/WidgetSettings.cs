using Files.App.Data.Enums;

namespace DesktopWidgets3.Models.Widget;

public class BaseWidgetSettings
{
    public virtual BaseWidgetSettings Clone()
    {
        return (BaseWidgetSettings)MemberwiseClone();
    }
}

public class ClockWidgetSettings : BaseWidgetSettings
{
    public bool ShowSeconds { get; set; } = true;

    public override BaseWidgetSettings Clone()
    {
        var clone = (ClockWidgetSettings)base.Clone();
        clone.ShowSeconds = ShowSeconds;
        return clone;
    }
}

public class DiskWidgetSettings : BaseWidgetSettings
{
    public override BaseWidgetSettings Clone()
    {
        var clone = (DiskWidgetSettings)base.Clone();
        return clone;
    }
}

public class FolderViewWidgetSettings : BaseWidgetSettings
{
    public string FolderPath { get; set; } = $"C:\\";

    public bool AllowNavigation { get; set; } = true;

    public bool MoveShellExtensionsToSubMenu { get; set; } = true;

    public bool SyncFolderPreferencesAcrossDirectories { get; set; } = false;

    public DetailsViewSizeKind DetailsViewSize { get; set; } = DetailsViewSizeKind.Small;

    public ListViewSizeKind ListViewSize { get; set; } = ListViewSizeKind.Small;

    public TilesViewSizeKind TilesViewSize { get; set; } = TilesViewSizeKind.Small;

    public GridViewSizeKind GridViewSize { get; set; } = GridViewSizeKind.Small;

    public ColumnsViewSizeKind ColumnsViewSize { get; set; } = ColumnsViewSizeKind.Small;

    public bool ShowHiddenItems { get; set; } = false;

    public bool ShowDotFiles { get; set; } = true;

    public bool ShowProtectedSystemFiles { get; set; } = false;

    public bool AreAlternateStreamsVisible { get; set; } = false;

    public bool ShowFileExtensions { get; set; } = true;

    public bool ShowThumbnails { get; set; } = true;

    public bool ShowCheckboxesWhenSelectingItems { get; set; } = true;

    public DeleteConfirmationPolicies DeleteConfirmationPolicy { get; set; } = DeleteConfirmationPolicies.Always;

    public bool ShowFileExtensionWarning { get; set; } = true;

    public FileNameConflictResolveOptionType ConflictsResolveOption { get; set; } = FileNameConflictResolveOptionType.GenerateNewName;

    public bool ShowRunningAsAdminPrompt { get; set; } = true;

    public override BaseWidgetSettings Clone()
    {
        var clone = (FolderViewWidgetSettings)base.Clone();
        clone.FolderPath = FolderPath;
        clone.AllowNavigation = AllowNavigation;
        clone.MoveShellExtensionsToSubMenu = MoveShellExtensionsToSubMenu;
        clone.SyncFolderPreferencesAcrossDirectories = SyncFolderPreferencesAcrossDirectories;
        clone.DetailsViewSize = DetailsViewSize;
        clone.ListViewSize = ListViewSize;
        clone.TilesViewSize = TilesViewSize;
        clone.GridViewSize = GridViewSize;
        clone.ColumnsViewSize = ColumnsViewSize;
        clone.ShowHiddenItems = ShowHiddenItems;
        clone.ShowDotFiles = ShowDotFiles;
        clone.ShowProtectedSystemFiles = ShowProtectedSystemFiles;
        clone.AreAlternateStreamsVisible = AreAlternateStreamsVisible;
        clone.ShowFileExtensions = ShowFileExtensions;
        clone.ShowThumbnails = ShowThumbnails;
        clone.ShowCheckboxesWhenSelectingItems = ShowCheckboxesWhenSelectingItems;
        clone.DeleteConfirmationPolicy = DeleteConfirmationPolicy;
        clone.ShowFileExtensionWarning = ShowFileExtensionWarning;
        clone.ConflictsResolveOption = ConflictsResolveOption;
        clone.ShowRunningAsAdminPrompt = ShowRunningAsAdminPrompt;
        return clone;
    }
}

public class NetworkWidgetSettings : BaseWidgetSettings
{
    public bool UseBps { get; set; } = false;

    public string HardwareIdentifier { get; set; } = "Total";

    public override BaseWidgetSettings Clone()
    {
        var clone = (NetworkWidgetSettings)base.Clone();
        clone.UseBps = UseBps;
        clone.HardwareIdentifier = HardwareIdentifier;
        return clone;
    }
}

public class PerformanceWidgetSettings : BaseWidgetSettings
{
    public bool UseCelsius { get; set; } = true;

    public override BaseWidgetSettings Clone()
    {
        var clone = (PerformanceWidgetSettings)base.Clone();
        clone.UseCelsius = UseCelsius;
        return clone;
    }
}