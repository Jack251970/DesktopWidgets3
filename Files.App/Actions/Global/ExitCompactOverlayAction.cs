// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;

namespace Files.App.Actions;

internal sealed class ExitCompactOverlayAction : ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IWindowContext windowContext;

	public string Label
		=> "ExitCompactOverlay".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconExitCompactOverlay");

	public HotKey HotKey
		=> new(Keys.Down, KeyModifiers.CtrlAlt);

	public string Description
		=> "ExitCompactOverlayDescription".GetLocalizedResource();

	public bool IsExecutable
		=> windowContext.IsCompactOverlay;

	public ExitCompactOverlayAction(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
        windowContext = folderViewViewModel.GetRequiredService<IWindowContext>();

        windowContext.PropertyChanged += WindowContext_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
        var appWindow = FolderViewViewModel.AppWindow;
		appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);

		return Task.CompletedTask;
	}

	private void WindowContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IWindowContext.IsCompactOverlay):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
