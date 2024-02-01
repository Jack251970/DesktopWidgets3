// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class OpenAllTaggedActions: ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IContentPageContext _pageContext;

	private readonly ITagsContext _tagsContext;

	public string Label
		=> "OpenAllTaggedItems".GetLocalizedResource();

	public string Description
		=> "OpenAllTaggedItemsDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new("\uE71D");

	public bool IsExecutable => 
		_pageContext.ShellPage is not null &&
		_tagsContext.TaggedItems.Any();

	public OpenAllTaggedActions(IFolderViewViewModel folderViewViewModel, IContentPageContext context)
	{
        FolderViewViewModel = folderViewViewModel;
        _pageContext = context;
		_tagsContext = DependencyExtensions.GetService<ITagsContext>();

		_pageContext.PropertyChanged += Context_PropertyChanged;
		_tagsContext.PropertyChanged += Context_PropertyChanged;
	}

	public async Task ExecuteAsync()
	{
		var files = _tagsContext.TaggedItems.Where(item => !item.isFolder);
		var folders = _tagsContext.TaggedItems.Where(item => item.isFolder);

		await Task.WhenAll(files.Select(file 
			=> NavigationHelpers.OpenPath(FolderViewViewModel, file.path, _pageContext.ShellPage!)));

		folders.ForEach(async folder => await NavigationHelpers.OpenPathInNewTab(FolderViewViewModel, folder.path));
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.ShellPage):
			case nameof(ITagsContext.TaggedItems):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}

