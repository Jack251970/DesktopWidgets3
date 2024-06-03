// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class NewWindowAction : IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    public string Label
		=> "NewWindow".GetLocalizedResource();

	public string Description
		=> "NewWindowDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.N, KeyModifiers.Ctrl);

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconOpenNewWindow");

	public NewWindowAction(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;
	}

	public Task ExecuteAsync()
	{
		return NavigationHelpers.LaunchNewWindowAsync(FolderViewViewModel);
	}
}
