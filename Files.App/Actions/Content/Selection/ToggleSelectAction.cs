// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Actions;

internal class ToggleSelectAction : IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    public string Label
		=> "ToggleSelect".GetLocalizedResource();

	public string Description
		=> "ToggleSelectDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.Space, KeyModifiers.Ctrl);

	public bool IsExecutable
		=> GetFocusedElement(FolderViewViewModel) is not null;

    public ToggleSelectAction(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
    }

	public Task ExecuteAsync()
	{
		if (GetFocusedElement(FolderViewViewModel) is SelectorItem item)
        {
            item.IsSelected = !item.IsSelected;
        }

        return Task.CompletedTask;
	}

	private static SelectorItem? GetFocusedElement(IFolderViewViewModel folderViewViewModel)
	{
		return FocusManager.GetFocusedElement(folderViewViewModel.MainWindow.Content.XamlRoot) as SelectorItem;
	}
}
