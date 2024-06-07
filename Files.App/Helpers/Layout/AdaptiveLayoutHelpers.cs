// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Previews;
using Files.Shared.Helpers;
using IniParser.Model;
using Windows.Storage;
using static Files.App.Constants.AdaptiveLayout;
using IO = System.IO;

namespace Files.App.Helpers;

public static class AdaptiveLayoutHelpers
{
    /*private static IFoldersSettingsService FoldersSettingsService { get; } = DependencyExtensions.GetService<IFoldersSettingsService>();*/
    private static ILayoutSettingsService LayoutSettingsService { get; } = DependencyExtensions.GetService<ILayoutSettingsService>();

    public static void ApplyAdaptativeLayout(IFolderViewViewModel folderViewViewModel, LayoutPreferencesManager folderSettings, string path, IList<ListedItem> filesAndFolders)
	{
        var LayoutSettingsService = folderViewViewModel.GetService<ILayoutSettingsService>();
		
        if (LayoutSettingsService.SyncFolderPreferencesAcrossDirectories)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (folderSettings.IsLayoutModeFixed || !folderSettings.IsAdaptiveLayoutEnabled)
        {
            return;
        }

        var layout = GetAdaptiveLayout(path, filesAndFolders);
		switch (layout)
		{
			case Layouts.Detail:
				folderSettings.ToggleLayoutModeDetailsView(false);
				break;
			case Layouts.Grid:
                folderSettings.ToggleLayoutModeGridView(false);
                break;
        }
	}

	private static Layouts GetAdaptiveLayout(string path, IList<ListedItem> filesAndFolders)
	{
		var pathLayout = GetPathLayout(path);
		if (pathLayout is not Layouts.None)
        {
            return pathLayout;
        }

        return GetContentLayout(filesAndFolders);
	}

	private static Layouts GetPathLayout(string path)
	{
		var iniPath = IO.Path.Combine(path, "desktop.ini");

        var iniContents = Win32Helper.ReadStringFromFile(iniPath)?.Trim();
        if (string.IsNullOrEmpty(iniContents))
        {
            return Layouts.None;
        }

        var parser = new IniParser.Parser.IniDataParser();
		parser.Configuration.ThrowExceptionsOnError = false;
		var data = parser.Parse(iniContents);
		if (data is null)
        {
            return Layouts.None;
        }

        var viewModeSection = data.Sections.FirstOrDefault(IsViewState);
		if (viewModeSection is null)
        {
            return Layouts.None;
        }

        var folderTypeKey = viewModeSection.Keys.FirstOrDefault(IsFolderType);
		if (folderTypeKey is null)
        {
            return Layouts.None;
        }

        return folderTypeKey.Value switch
		{
			"Pictures" => Layouts.Grid,
			"Videos" => Layouts.Grid,
			_ => Layouts.Detail,
		};

		static bool IsViewState(SectionData data)
			=> "ViewState".Equals(data.SectionName, StringComparison.OrdinalIgnoreCase);

		static bool IsFolderType(KeyData data)
			=> "FolderType".Equals(data.KeyName, StringComparison.OrdinalIgnoreCase);
	}

	private static Layouts GetContentLayout(IList<ListedItem> filesAndFolders)
	{
		var itemCount = filesAndFolders.Count;
		if (filesAndFolders.Count is 0)
        {
            return Layouts.None;
        }

        var folderPercentage = 100f * filesAndFolders.Count(IsFolder) / itemCount;
		var imagePercentage = 100f * filesAndFolders.Count(IsImage) / itemCount;
		var mediaPercentage = 100f * filesAndFolders.Count(IsMedia) / itemCount;
		var miscPercentage = 100f - (folderPercentage + imagePercentage + mediaPercentage);

		if (folderPercentage + miscPercentage > LargeThreshold)
        {
            return Layouts.Detail;
        }

        if (imagePercentage > ExtraLargeThreshold)
        {
            return Layouts.Grid;
        }

        if (imagePercentage <= MediumThreshold)
        {
            return Layouts.Detail;
        }

        if (100f - imagePercentage <= SmallThreshold)
        {
            return Layouts.Detail;
        }

        if (folderPercentage + miscPercentage <= ExtraSmallThreshold)
        {
            return Layouts.Detail;
        }

        return Layouts.Grid;

		static bool IsFolder(ListedItem item)
			=> item.PrimaryItemAttribute is StorageItemTypes.Folder;

		static bool IsImage(ListedItem item)
			=> !string.IsNullOrEmpty(item.FileExtension)
			&& ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());

		static bool IsMedia(ListedItem item)
			=> !string.IsNullOrEmpty(item.FileExtension)
			&& (FileExtensionHelpers.IsAudioFile(item.FileExtension) 
			|| FileExtensionHelpers.IsVideoFile(item.FileExtension));
	}

	private enum Layouts
	{
		None, // Don't decide. Another function to decide can be called afterwards if available.
		Detail, // Apply the layout Detail.
		Grid, // Apply the layout Grid.
	}
}
