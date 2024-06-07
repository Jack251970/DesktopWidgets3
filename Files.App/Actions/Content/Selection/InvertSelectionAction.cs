// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class InvertSelectionAction(IContentPageContext context) : IAction
{
	private readonly IContentPageContext context = context;

	public string Label
		=> "InvertSelection".GetLocalizedResource();

	public string Description
		=> "InvertSelectionDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new("\uE746");

	public bool IsExecutable
	{
		get
		{
			if (context.PageType is ContentPageTypes.Home)
            {
                return false;
            }

            if (!context.HasItem)
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
		context?.ShellPage?.SlimContentPage?.ItemManipulationModel?.InvertSelection();

		return Task.CompletedTask;
	}
}
