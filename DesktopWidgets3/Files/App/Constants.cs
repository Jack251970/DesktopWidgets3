namespace Files.App;

public class Constants
{
    public static class UI
    {
        public const float DimItemOpacity = 0.4f;

        // For contextmenu hacks, must match WinUI style
        public const double ContextMenuMaxHeight = 480;

        // For contextmenu hacks, must match WinUI style
        public const double ContextMenuSecondaryItemsHeight = 32;

        // For contextmenu hacks, must match WinUI style
        public const double ContextMenuPrimaryItemsHeight = 48;

        // For contextmenu hacks
        public const double ContextMenuLabelMargin = 10;

        // For contextmenu hacks
        public const double ContextMenuItemsMaxWidth = 250;
    }

    public static class UserEnvironmentPaths
    {
        public static readonly string DesktopPath = Windows.Storage.UserDataPaths.GetDefault().Desktop;

        public static readonly string DownloadsPath = Windows.Storage.UserDataPaths.GetDefault().Downloads;

        public static readonly string LocalAppDataPath = Windows.Storage.UserDataPaths.GetDefault().LocalAppData;

        // Currently is the command to open the folder from cmd ("cmd /c start Shell:RecycleBinFolder")
        public const string RecycleBinPath = @"Shell:RecycleBinFolder";

        public const string NetworkFolderPath = @"Shell:NetworkPlacesFolder";

        public const string MyComputerPath = @"Shell:MyComputerFolder";

        public static readonly string TempPath = Environment.GetEnvironmentVariable("TEMP") ?? "";

        public static readonly string HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static readonly string SystemRootPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        public static readonly string RecentItemsPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);

        public static Dictionary<string, string> ShellPlaces =
            new()
            {
                    { "::{645FF040-5081-101B-9F08-00AA002F954E}", RecycleBinPath },
                    { "::{5E5F29CE-E0A8-49D3-AF32-7A7BDC173478}", "Home" /*MyComputerPath*/ },
                    { "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", MyComputerPath },
                    { "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", NetworkFolderPath },
                    { "::{208D2C60-3AEA-1069-A2D7-08002B30309D}", NetworkFolderPath },
                    { RecycleBinPath.ToUpperInvariant(), RecycleBinPath },
                    { MyComputerPath.ToUpperInvariant(), MyComputerPath },
                    { NetworkFolderPath.ToUpperInvariant(), NetworkFolderPath },
            };
    }

    public static class ResourceFilePaths
    {
        /// <summary>
        /// The path to the json file containing a list of file properties to be loaded in the properties window details page.
        /// </summary>
        public const string DetailsPagePropertiesJsonPath = @"ms-appx:///Resources/PropertiesInformation.json";

        /// <summary>
        /// The path to the json file containing a list of file properties to be loaded in the preview pane.
        /// </summary>
        public const string PreviewPaneDetailsPropertiesJsonPath = @"ms-appx:///Resources/PreviewPanePropertiesInformation.json";
    }
}
