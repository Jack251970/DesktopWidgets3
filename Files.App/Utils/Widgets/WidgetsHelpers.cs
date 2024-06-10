// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers;

public static class WidgetsHelpers
{
	public static bool TryGetWidget<TWidget>(HomeViewModel widgetsViewModel) where TWidget : IWidgetViewModel, new()
    {
        var canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
        var isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidget>(widgetsViewModel.FolderViewViewModel);

        if (canAddWidget && isWidgetSettingEnabled)
        {
            return true;
        }
        // The widgets exists but the setting has been disabled for it
        else if (!canAddWidget && !isWidgetSettingEnabled)
        {
            // Remove the widget
            widgetsViewModel.RemoveWidget<TWidget>();
            return false;
        }
        else if (!isWidgetSettingEnabled)
        {
            return false;
        }

        return true;
    }

    public static bool TryGetIsWidgetSettingEnabled<TWidget>(IFolderViewViewModel folderViewViewModel) where TWidget : IWidgetViewModel
    {
        var generalSettingsService = folderViewViewModel.GetRequiredService<IGeneralSettingsService>();

        if (typeof(TWidget) == typeof(QuickAccessWidgetViewModel))
        {
            return generalSettingsService.ShowQuickAccessWidget;
        }
        if (typeof(TWidget) == typeof(DrivesWidgetViewModel))
        {
            return generalSettingsService.ShowDrivesWidget;
        }
        if (typeof(TWidget) == typeof(FileTagsWidgetViewModel))
        {
            return generalSettingsService.ShowFileTagsWidget;
        }
        if (typeof(TWidget) == typeof(RecentFilesWidgetViewModel))
        {
            return generalSettingsService.ShowRecentFilesWidget;
        }

        return false;
    }
}
