// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class OpenInNewPaneFromHomeAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : BaseOpenInNewPaneAction(folderViewViewModel, context)
{
	public override bool IsExecutable =>
		UserSettingsService.GeneralSettingsService.ShowOpenInNewPane &&
		HomePageContext.IsAnyItemRightClicked &&
		HomePageContext.RightClickedItem is not null &&
		(HomePageContext.RightClickedItem is not WidgetFileTagCardItem fileTagItem
            || fileTagItem.IsFolder);

	public override bool IsAccessibleGlobally
		=> false;

	public async override Task ExecuteAsync(object? parameter = null)
	{
		if (HomePageContext.RightClickedItem is null)
        {
            return;
        }

        if (await DriveHelpers.CheckEmptyDrive(FolderViewViewModel, HomePageContext.RightClickedItem!.Path))
        {
            return;
        }

        ContentPageContext.ShellPage!.PaneHolder?.OpenSecondaryPane(HomePageContext.RightClickedItem!.Path ?? string.Empty);
	}

	protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IHomePageContext.IsAnyItemRightClicked):
			case nameof(IHomePageContext.RightClickedItem):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
