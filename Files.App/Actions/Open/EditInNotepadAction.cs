// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions;

internal sealed class EditInNotepadAction : ObservableObject, IAction
{
	private readonly IContentPageContext _context;

	public string Label
		=> "EditInNotepad".GetLocalizedResource();

	public string Description
		=> "EditInNotepadDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new("\uE70F");

	public bool IsExecutable =>
		_context.SelectedItems.Any() &&
		_context.PageType != ContentPageTypes.RecycleBin &&
		_context.PageType != ContentPageTypes.ZipFolder &&
		_context.SelectedItems.All(x => FileExtensionHelpers.IsBatchFile(x.FileExtension) || FileExtensionHelpers.IsAhkFile(x.FileExtension) || FileExtensionHelpers.IsCmdFile(x.FileExtension));

	public EditInNotepadAction(IContentPageContext context)
    {
        _context = context;

        _context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
        // TODO: Optimize opening explore codes like this.
		return Task.WhenAll(_context.SelectedItems.Select(item => Win32Helper.RunPowershellCommandAsync($"notepad '{item.ItemPath}\'", false)));
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.SelectedItems):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}