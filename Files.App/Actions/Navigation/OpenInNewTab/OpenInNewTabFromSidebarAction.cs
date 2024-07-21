// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class OpenInNewTabFromSidebarAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : BaseOpenInNewTabAction(folderViewViewModel, context)
{
	public override bool IsExecutable =>
		UserSettingsService.GeneralSettingsService.ShowOpenInNewTab &&
		SidebarContext.IsItemRightClicked &&
		SidebarContext.RightClickedItem is not null &&
		SidebarContext.RightClickedItem.MenuOptions.IsLocationItem;

	public override bool IsAccessibleGlobally
		=> false;

	public async override Task ExecuteAsync(object? parameter = null)
	{
		if (SidebarContext.RightClickedItem is null)
        {
            return;
        }

        if (await DriveHelpers.CheckEmptyDrive(FolderViewViewModel, SidebarContext.RightClickedItem!.Path))
        {
            return;
        }

        await NavigationHelpers.OpenPathInNewTab(FolderViewViewModel, SidebarContext.RightClickedItem!.Path ?? string.Empty, false);
	}

	protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ISidebarContext.IsItemRightClicked):
			case nameof(ISidebarContext.RightClickedItem):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
