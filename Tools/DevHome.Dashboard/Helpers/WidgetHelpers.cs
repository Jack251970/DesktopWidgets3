// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
//using DevHome.Common.Extensions;
//using DevHome.Common.Services;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Helpers;

public sealed class WidgetHelpers
{
    public const string WebExperiencePackPackageId = "9MSSGKG348SP";
    public const string WebExperiencePackageFamilyName = "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy";
    public const string WidgetsPlatformRuntimePackageId = "9N3RK8ZV2ZR8";
    public const string WidgetsPlatformRuntimePackageFamilyName = "Microsoft.WidgetsPlatformRuntime_8wekyb3d8bbwe";

    // TODO(Future): Remove this and use our own host name.
    public const string DevHomeHostName = "DevHome";

    // Each widget has a 16px margin around it and a 48px Attribution area in which content cannot be placed.
    // https://learn.microsoft.com/en-us/windows/apps/design/widgets/widgets-design-fundamentals
    // Adaptive cards render with 8px padding on each side, so we subtract that from the header height.
    public const double HeaderHeightUnscaled = 40;

    public const double WidgetPxHeightSmall = 146;
    public const double WidgetPxHeightMedium = 304;
    public const double WidgetPxHeightLarge = 462;

    public const double WidgetPxWidth = 300;

    public static WidgetSize GetLargestCapabilitySize(WidgetCapability[] capabilities)
    {
        // Guaranteed to have at least one capability
        var largest = capabilities[0].Size;

        foreach (var cap in capabilities)
        {
            if (cap.Size > largest)
            {
                largest = cap.Size;
            }
        }

        return largest;
    }

    public static WidgetSize GetDefaultWidgetSize(WidgetCapability[] capabilities)
    {
        // The default size of the widget should be prioritized as Medium, Large, Small.
        // This matches the size preferences of the Windows Widget Dashboard.
        if (capabilities.Any(cap => cap.Size == WidgetSize.Medium))
        {
            return WidgetSize.Medium;
        }
        else if (capabilities.Any(cap => cap.Size == WidgetSize.Large))
        {
            return WidgetSize.Large;
        }
        else if (capabilities.Any(cap => cap.Size == WidgetSize.Small))
        {
            return WidgetSize.Small;
        }
        else
        {
            // Return something in case new sizes are added.
            return capabilities[0].Size;
        }
    }

    public static async Task<bool> IsIncludedWidgetProviderAsync(WidgetProviderDefinition provider)
    {
        /*// Cut WidgetProviderDefinition id down to just the package family name.
        var providerId = provider.Id;
        var endOfPfnIndex = providerId.IndexOf('!', StringComparison.Ordinal);
        var familyNamePartOfProviderId = providerId[..endOfPfnIndex];

        // Get the list of packages that contain Dev Home widgets and are enabled.
        var extensionService = DependencyExtensions.GetRequiredService<IExtensionService>();
        var enabledWidgetProviderIds = await extensionService.GetInstalledDevHomeWidgetPackageFamilyNamesAsync(includeDisabledExtensions: false);

        // Check if the specified widget provider is in the list.
        var include = enabledWidgetProviderIds.ToList().Contains(familyNamePartOfProviderId);
        LogExtensions.LogInformation(ClassName, $"Found provider Id = {providerId}, include = {include}");
        return include;*/
        if (provider.DisplayName == "PeregrineWidgets")
        {
            return false;
        }

        await Task.CompletedTask;
        return true;
    }

    public static string CreateWidgetCustomState(int ordinal)
    {
        var state = new WidgetCustomState
        {
            Host = DevHomeHostName,
            Position = ordinal,
        };

        return JsonSerializer.Serialize(state, SourceGenerationContext.Default.WidgetCustomState);
    }
}
