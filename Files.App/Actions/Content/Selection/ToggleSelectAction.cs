// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Actions;

internal sealed class ToggleSelectAction(IFolderViewViewModel folderViewViewModel) : IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel = folderViewViewModel;

    public string Label
		=> "ToggleSelect".GetLocalizedResource();

	public string Description
		=> "ToggleSelectDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.Space, KeyModifiers.Ctrl);

	public bool IsExecutable
		=> GetFocusedElement(FolderViewViewModel) is not null;

    public Task ExecuteAsync(object? parameter = null)
	{
		if (GetFocusedElement(FolderViewViewModel) is SelectorItem item)
        {
            item.IsSelected = !item.IsSelected;
        }

        return Task.CompletedTask;
	}

	private static SelectorItem? GetFocusedElement(IFolderViewViewModel folderViewViewModel)
	{
		return FocusManager.GetFocusedElement(folderViewViewModel.XamlRoot) as SelectorItem;
	}
}
