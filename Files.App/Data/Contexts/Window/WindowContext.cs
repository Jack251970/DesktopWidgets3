// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;

namespace Files.App.Data.Contexts;

internal class WindowContext : ObservableObject, IWindowContext
{
	private bool isCompactOverlay;
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
