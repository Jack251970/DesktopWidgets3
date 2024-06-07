// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class OpenPropertiesAction : ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IContentPageContext context;

	public string Label
		=> "OpenProperties".GetLocalizedResource();

	public string Description
		=> "OpenPropertiesDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconProperties");

	public HotKey HotKey
		=> new(Keys.Enter, KeyModifiers.Alt);

	public bool IsExecutable =>
		context.PageType is not ContentPageTypes.Home &&
		!(context.PageType is ContentPageTypes.SearchResults && 
		!context.HasSelection);

	public OpenPropertiesAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context)
    {
        FolderViewViewModel = folderViewViewModel;

        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
		var page = context.ShellPage?.SlimContentPage;

		if (page?.ItemContextMenuFlyout.IsOpen ?? false)
        {
            page.ItemContextMenuFlyout.Closed += OpenPropertiesFromItemContextMenuFlyout;
        }
        else if (page?.BaseContextMenuFlyout.IsOpen ?? false)
        {
            page.BaseContextMenuFlyout.Closed += OpenPropertiesFromBaseContextMenuFlyout;
        }
        else
        {
            FilePropertiesHelpers.OpenPropertiesWindow(FolderViewViewModel, context.ShellPage!);
        }

        return Task.CompletedTask;
	}

	private void OpenPropertiesFromItemContextMenuFlyout(object? _, object e)
	{
		var page = context.ShellPage?.SlimContentPage;
		if (page is not null)
        {
            page.ItemContextMenuFlyout.Closed -= OpenPropertiesFromItemContextMenuFlyout;
        }

        FilePropertiesHelpers.OpenPropertiesWindow(FolderViewViewModel, context.ShellPage!);
	}

	private void OpenPropertiesFromBaseContextMenuFlyout(object? _, object e)
	{
		var page = context.ShellPage?.SlimContentPage;
		if (page is not null)
        {
            page.BaseContextMenuFlyout.Closed -= OpenPropertiesFromBaseContextMenuFlyout;
        }

        FilePropertiesHelpers.OpenPropertiesWindow(FolderViewViewModel, context.ShellPage!);
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.PageType):
			case nameof(IContentPageContext.HasSelection):
			case nameof(IContentPageContext.Folder):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
