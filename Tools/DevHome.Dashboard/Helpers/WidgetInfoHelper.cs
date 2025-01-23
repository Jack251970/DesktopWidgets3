using DevHome.Dashboard.ComSafeWidgetObjects;
using DevHome.Dashboard.ViewModels;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Helpers;

public static class WidgetInfoHelper
{
    public static (string WidgetGroupName, string WidgetName, string WidgetDescription, string WidgetId, string WidgetType) GetWidgetProviderAndWidgetInfo(this WidgetViewModel widgetViewModel)
    {
        return GetWidgetProviderAndWidgetInfo(widgetViewModel.WidgetDefinition);
    }

    public static (string WidgetGroupName, string WidgetId) GetWidgetProviderInfo(this WidgetViewModel widgetViewModel)
    {
        return GetWidgetProviderInfo(widgetViewModel.WidgetDefinition.ProviderDefinition);
    }

    public static (string WidgetName, string WidgetDescription, string WidgetType) GetWidgetInfo(this WidgetViewModel widgetViewModel)
    {
        return GetWidgetInfo(widgetViewModel.WidgetDefinition);
    }

    public static (string WidgetGroupName, string WidgetName, string WidgetDescription, string WidgetId, string WidgetType) GetWidgetProviderAndWidgetInfo(this ComSafeWidgetDefinition comSafeWidgetDefinition)
    {
        if (comSafeWidgetDefinition != null)
        {
            var (widgetName, widgetDescription, widgetType) = GetWidgetInfo(comSafeWidgetDefinition);
            var (widgetGroupName, widgetId) = GetWidgetProviderInfo(comSafeWidgetDefinition.ProviderDefinition);
            return (widgetGroupName, widgetName, widgetDescription, widgetId, widgetType);
        }

        return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
    }

    public static (string WidgetGroupName, string WidgetId) GetWidgetProviderInfo(this WidgetProviderDefinition? widgetProviderDefinition)
    {
        if (widgetProviderDefinition != null)
        {
            var widgetGroupName = widgetProviderDefinition.DisplayName;
            var widgetId = widgetProviderDefinition.Id;

            return (widgetGroupName, widgetId);
        }

        return (string.Empty, string.Empty);
    }

    public static (string WidgetName, string WidgetDescription, string WidgetType) GetWidgetInfo(this ComSafeWidgetDefinition? comSafeWidgetDefinition)
    {
        if (comSafeWidgetDefinition != null)
        {
            var widgetName = comSafeWidgetDefinition.DisplayTitle;
            var widgetDescription = comSafeWidgetDefinition.Description;
            var widgetType = comSafeWidgetDefinition.Id;
            return (widgetName, widgetDescription, widgetType);
        }

        return (string.Empty, string.Empty, string.Empty);
    }

    public static string GetFamilyName(this WidgetProviderDefinition widgetProviderDefinition)
    {
        // Cut WidgetProviderDefinition id down to just the package family name.
        var providerId = widgetProviderDefinition.Id;
        var endOfPfnIndex = providerId.IndexOf('!', StringComparison.Ordinal);
        var familyNamePartOfProviderId = providerId[..endOfPfnIndex];
        return familyNamePartOfProviderId;
    }
}
