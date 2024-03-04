using Files.Core.Data.Enums;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService : IWidgetResourceService
{
    public string GetWidgetLabel(WidgetType widgetType)
    {
        return widgetType switch
        {
            _ => $"Widget_{widgetType}_Label".GetLocalized(),
        };
    }

    public string GetWidgetIconSource(WidgetType widgetType)
    {
        return widgetType switch
        {
            _ => $"ms-appx:///Assets/FluentIcons/{widgetType}.png"
        };
    }

    public WidgetSize GetDefaultSize(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.Clock => new WidgetSize(240, 240),
            WidgetType.FolderView => new WidgetSize(575, 480),
            _ => new WidgetSize(318, 200),
        }; ;
    }

    public WidgetSize GetMinSize(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.Clock => new WidgetSize(240, 240),
            WidgetType.FolderView => new WidgetSize(516, 416),
            _ => new WidgetSize(318, 200),
        };
    }

    public BaseWidgetSettings GetDefaultSettings(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.Clock => new ClockWidgetSettings()
            {
                ShowSeconds = true,
            },
            WidgetType.Disk => new DiskWidgetSettings()
            {

            },
            WidgetType.FolderView => new FolderViewWidgetSettings()
            {
                FolderPath = "C:\\",
                AllowNavigation = true,
                MoveShellExtensionsToSubMenu = true,
                SyncFolderPreferencesAcrossDirectories = false,
                ShowHiddenItems = false,
                ShowDotFiles = true,
                ShowProtectedSystemFiles = false,
                AreAlternateStreamsVisible = false,
                ShowFileExtensions = true,
                ShowThumbnails = true,
                ShowCheckboxesWhenSelectingItems = true,
                DeleteConfirmationPolicy = DeleteConfirmationPolicies.Always,
                ShowFileExtensionWarning = true,
                ConflictsResolveOption = FileNameConflictResolveOptionType.GenerateNewName,
                ShowRunningAsAdminPrompt = true,
            },
            WidgetType.Network => new NetworkWidgetSettings()
            {
                ShowBps = false,
            },
            WidgetType.Performance => new PerformanceWidgetSettings()
            {
                UseCelsius = true,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(widgetType), widgetType, null),
        };
    }
}
