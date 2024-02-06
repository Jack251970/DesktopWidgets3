// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class NewTabAction : IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    public string Label
		=> "NewTab".GetLocalizedResource();

	public string Description
		=> "NewTabDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.T, KeyModifiers.Ctrl);

	public NewTabAction(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;
	}

	public Task ExecuteAsync()
	{
		return NavigationHelpers.AddNewTabAsync(FolderViewViewModel);
	}
}
