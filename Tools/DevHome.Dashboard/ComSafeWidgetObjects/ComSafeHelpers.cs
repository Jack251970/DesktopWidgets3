// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Dashboard.Services;

namespace DevHome.Dashboard.ComSafeWidgetObjects;

public sealed class ComSafeHelpers
{
    public static async Task<List<ComSafeWidgetDefinition>> GetAllOrderedComSafeWidgetDefinitions(IWidgetHostingService widgetHostingService)
    {
        var unsafeWidgetDefinitions = await widgetHostingService.GetWidgetDefinitionsAsync();
        var comSafeWidgetDefinitions = new List<ComSafeWidgetDefinition>();
        foreach (var unsafeWidgetDefinition in unsafeWidgetDefinitions)
        {
            var id = await ComSafeWidgetDefinition.GetIdFromUnsafeWidgetDefinitionAsync(unsafeWidgetDefinition);
            if (!string.IsNullOrEmpty(id))
            {
                var comSafeWidgetDefinition = new ComSafeWidgetDefinition(id);
                if (await comSafeWidgetDefinition.PopulateAsync())
                {
                    comSafeWidgetDefinitions.Add(comSafeWidgetDefinition);
                }
            }
        }

        comSafeWidgetDefinitions = [.. comSafeWidgetDefinitions.OrderBy(def => def.DisplayTitle)];
        return comSafeWidgetDefinitions;
    }
}
