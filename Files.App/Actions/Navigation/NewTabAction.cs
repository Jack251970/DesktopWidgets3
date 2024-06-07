// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class NewTabAction(IFolderViewViewModel folderViewViewModel) : IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel = folderViewViewModel;

    public string Label
		=> "NewTab".GetLocalizedResource();

	public string Description
		=> "NewTabDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.T, KeyModifiers.Ctrl);

    public Task ExecuteAsync(object? parameter = null)
	{
		return NavigationHelpers.AddNewTabAsync(FolderViewViewModel);
	}
}
