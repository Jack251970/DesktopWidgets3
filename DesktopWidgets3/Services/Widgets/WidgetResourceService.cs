using Files.App.Data.Enums;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetResourceService(IAppSettingsService appSettingsService) : IWidgetResourceService
{
    private readonly IAppSettingsService _appSettingsService = appSettingsService;

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

    public static RectSize GetDefaultSize(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.Clock => new RectSize(280, 280),
            WidgetType.Disk => new RectSize(345, 200),
            WidgetType.FolderView => new RectSize(575, 480),
            WidgetType.Network => new RectSize(345, 150),
            WidgetType.Performance => new RectSize(345, 200),
            _ => new RectSize(318, 200),
        }; ;
    }

    public RectSize GetMinSize(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.Clock => new RectSize(240, 240),
            WidgetType.FolderView => new RectSize(516, 416),
            WidgetType.Network => new RectSize(300, 150),
            _ => new RectSize(318, 200),
        };
    }

    public static BaseWidgetSettings GetDefaultSettings(WidgetType widgetType)
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
                DetailsViewSize = DetailsViewSizeKind.Small,
                ListViewSize = ListViewSizeKind.Small,
                TilesViewSize = TilesViewSizeKind.Small,
                GridViewSize = GridViewSizeKind.Small,
                ColumnsViewSize = ColumnsViewSizeKind.Small,
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
                UseBps = false,
            },
            WidgetType.Performance => new PerformanceWidgetSettings()
            {
                UseCelsius = true,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(widgetType), widgetType, null),
        };
    }

    public bool GetWidgetInNewThread(WidgetType widgetType)
    {
        if (!_appSettingsService.MultiThread)
        {
            return false;
        }

        return widgetType switch
        {
            WidgetType.Clock => true,
            WidgetType.Disk => true,
            WidgetType.Network => true,
            WidgetType.Performance => true,
            _ => false,
        };
    }
}
