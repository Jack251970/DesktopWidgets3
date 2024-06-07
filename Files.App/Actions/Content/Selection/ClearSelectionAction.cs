// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class ClearSelectionAction(IContentPageContext context) : IAction
{
	private readonly IContentPageContext context = context;

	public string Label
		=> "ClearSelection".GetLocalizedResource();

	public string Description
		=> "ClearSelectionDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new("\uE8E6");

	public bool IsExecutable
	{
		get
		{
			if (context.PageType is ContentPageTypes.Home)
            {
                return false;
            }

            if (!context.HasSelection)
            {
                return false;
            }

            var page = context.ShellPage;
            if (page is null)
            {
                return false;
            }

            var isCommandPaletteOpen = page.ToolbarViewModel.IsCommandPaletteOpen;
			var isEditing = page.ToolbarViewModel.IsEditModeEnabled;
			var isRenaming = page.SlimContentPage.IsRenamingItem;

			return isCommandPaletteOpen || (!isEditing && !isRenaming);
		}
	}

    public Task ExecuteAsync(object? parameter = null)
	{
		context.ShellPage?.SlimContentPage?.ItemManipulationModel?.ClearSelection();

		return Task.CompletedTask;
	}
}
