// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class OpenCommandPaletteAction(IContentPageContext context) : IAction
{
	private readonly IContentPageContext _context = context;

	public string Label
		=> "CommandPalette".GetLocalizedResource();

	public string Description
		=> "OpenCommandPaletteDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.P, KeyModifiers.CtrlShift);

    public Task ExecuteAsync(object? parameter = null)
	{
		_context.ShellPage?.ToolbarViewModel.OpenCommandPalette();

		return Task.CompletedTask;
	}
}
