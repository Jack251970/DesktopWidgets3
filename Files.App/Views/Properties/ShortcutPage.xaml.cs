// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;

namespace Files.App.Views.Properties;

public sealed partial class ShortcutPage : BasePropertiesPage
{
	public ShortcutPage()
	{
		InitializeComponent();
	}

	public async override Task<bool> SaveChangesAsync()
	{
        if (BaseProperties switch
        {
            FileProperties properties => properties.Item,
            FolderProperties properties => properties.Item,
            _ => null
        } is not ShortcutItem shortcutItem)
        {
            return true;
        }

        ViewModel.RunAsAdmin = ViewModel.RunAsAdminEditedValue;
        ViewModel.ShortcutItemPath = ViewModel.ShortcutItemPathEditedValue;
        ViewModel.ShortcutItemWorkingDir = ViewModel.ShortcutItemWorkingDirEditedValue;
        ViewModel.ShortcutItemArguments = ViewModel.ShortcutItemArgumentsEditedValue;

        await ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(() =>
			UIFilesystemHelpers.UpdateShortcutItemProperties(shortcutItem,
			ViewModel.ShortcutItemPath,
			ViewModel.ShortcutItemArguments,
			ViewModel.ShortcutItemWorkingDir,
			ViewModel.RunAsAdmin)
		);

		return true;
	}

	public override void Dispose()
	{
	}
}
