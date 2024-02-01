// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class SelectAllAction : IAction
{
	private readonly IContentPageContext context;

	public string Label
		=> "SelectAll".GetLocalizedResource();

	public string Description
		=> "SelectAllDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new("\uE8B3");

	public HotKey HotKey
		=> new(Keys.A, KeyModifiers.Ctrl);

	public bool IsExecutable
	{
		get
		{
			if (context.PageType is ContentPageTypes.Home)
            {
                return false;
            }

            var page = context.ShellPage;
			if (page is null)
            {
                return false;
            }

            var itemCount = page.FilesystemViewModel.FilesAndFolders.Count;
			var selectedItemCount = context.SelectedItems.Count;
			if (itemCount == selectedItemCount)
            {
                return false;
            }

            var isCommandPaletteOpen = page.ToolbarViewModel.IsCommandPaletteOpen;
			var isEditing = page.ToolbarViewModel.IsEditModeEnabled;
			var isRenaming = page.SlimContentPage?.IsRenamingItem ?? false;

			return isCommandPaletteOpen || (!isEditing && !isRenaming);
		}
	}

	public SelectAllAction(IContentPageContext context)
    {
        this.context = context;
    }

	public Task ExecuteAsync()
	{
		context.ShellPage?.SlimContentPage?.ItemManipulationModel?.SelectAllItems();

		return Task.CompletedTask;
	}
}
