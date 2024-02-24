﻿using Files.Core.Data.Enums;

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

    public bool ShowHiddenFile { get; set; } = false;

    public bool ShowExtension { get; set; } = false;

    public bool ShowThumbnail { get; set; } = true;

    public DeleteConfirmationPolicies DeleteConfirmationPolicy { get; set; } = DeleteConfirmationPolicies.Always;

    public FileNameConflictResolveOptionType ConflictsResolveOption { get; set; } = FileNameConflictResolveOptionType.GenerateNewName;

    public bool ShowRunningAsAdminPrompt { get; set; } = true;

    public override BaseWidgetSettings Clone()
    {
        var clone = (FolderViewWidgetSettings)base.Clone();
        clone.FolderPath = FolderPath;
        clone.AllowNavigation = AllowNavigation;
        clone.MoveShellExtensionsToSubMenu = MoveShellExtensionsToSubMenu;
        clone.ShowHiddenFile = ShowHiddenFile;
        clone.ShowExtension = ShowExtension;
        clone.DeleteConfirmationPolicy = DeleteConfirmationPolicy;
        clone.ShowThumbnail = ShowThumbnail;
        clone.ConflictsResolveOption = ConflictsResolveOption;
        clone.ShowRunningAsAdminPrompt = ShowRunningAsAdminPrompt;
        return clone;
    }
}

public class NetworkWidgetSettings : BaseWidgetSettings
{
    public bool ShowBps { get; set; } = false;

    public override BaseWidgetSettings Clone()
    {
        var clone = (NetworkWidgetSettings)base.Clone();
        clone.ShowBps = ShowBps;
        return clone;
    }
}

public class PerformanceWidgetSettings : BaseWidgetSettings
{
    public override BaseWidgetSettings Clone()
    {
        var clone = (PerformanceWidgetSettings)base.Clone();
        return clone;
    }
}