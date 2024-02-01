// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;

namespace Files.App.Actions;

internal class ToggleFullScreenAction : IToggleAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    public string Label
		=> "FullScreen".GetLocalizedResource();

	public string Description
		=> "ToggleFullScreenDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.F11);

	public bool IsOn
	{
		get
		{
			var appWindow = FolderViewViewModel.MainWindow.AppWindow;
			return appWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen;
		}
	}

    public ToggleFullScreenAction(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
    }

	public Task ExecuteAsync()
	{
		var appWindow = FolderViewViewModel.MainWindow.AppWindow;
		var newKind = appWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen
			? AppWindowPresenterKind.Overlapped
			: AppWindowPresenterKind.FullScreen;

		appWindow.SetPresenter(newKind);
		return Task.CompletedTask;
	}
}
