// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;

namespace Files.App.Actions;

internal sealed class ToggleFullScreenAction(IFolderViewViewModel folderViewViewModel) : IToggleAction
{
    private readonly IFolderViewViewModel FolderViewViewModel = folderViewViewModel;

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
			var appWindow = FolderViewViewModel.AppWindow;
			return appWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen;
		}
	}

    public Task ExecuteAsync(object? parameter = null)
	{
		var appWindow = FolderViewViewModel.AppWindow;
		var newKind = appWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen
			? AppWindowPresenterKind.Overlapped
			: AppWindowPresenterKind.FullScreen;

		appWindow.SetPresenter(newKind);
		return Task.CompletedTask;
	}
}
