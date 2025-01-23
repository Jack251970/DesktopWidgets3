// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Helpers;

public sealed class WidgetHelpers
{
    public const string WebExperiencePackPackageId = "9MSSGKG348SP";
    public const string WebExperiencePackageFamilyName = "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy";
    public const string WidgetsPlatformRuntimePackageId = "9N3RK8ZV2ZR8";
    public const string WidgetsPlatformRuntimePackageFamilyName = "Microsoft.WidgetsPlatformRuntime_8wekyb3d8bbwe";

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

    public static bool IsIncludedWidgetProvider(WidgetProviderDefinition provider)
    {
        // The family name part of the provider ID is used to determine if the widget provider is included.
        var familyNamePartOfProviderId = provider.GetFamilyName();

        // Check if the specified widget provider is not the WebExperiencePackage.
        // Theoretically, the widgets of the WebExperiencePackage should work, but it cause COM issues for icons & screenshots.
        // And even the widgets cannot be created. So, we exclude them.
        return familyNamePartOfProviderId != WebExperiencePackageFamilyName;
    }

    public static string CreateWidgetCustomState(int ordinal)
    {
        var state = new WidgetCustomState
        {
            Host = Constants.MicrosoftWidgetHostName,
            Position = ordinal,
        };

        return JsonSerializer.Serialize(state, SourceGenerationContext.Default.WidgetCustomState);
    }
}
