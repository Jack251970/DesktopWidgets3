// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;

namespace Files.App.Data.Contexts;

internal sealed class WindowContext : ObservableObject, IWindowContext
{
    // CHANGE: Remove compact overlay.
    private bool isCompactOverlay = true;
	public bool IsCompactOverlay => isCompactOverlay;

	public WindowContext()
	{
		
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        folderViewViewModel.MainWindow.PresenterChanged += Window_PresenterChanged;
    }

	private void Window_PresenterChanged(object? sender, AppWindowPresenter e)
	{
		SetProperty(
			ref isCompactOverlay,
			e.Kind is AppWindowPresenterKind.CompactOverlay,
			nameof(IsCompactOverlay));
	}
}
