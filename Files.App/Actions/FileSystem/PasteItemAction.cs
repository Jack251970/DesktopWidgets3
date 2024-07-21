﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class PasteItemAction : ObservableObject, IAction
{
    private readonly IContentPageContext context;

	public string Label
		=> "Paste".GetLocalizedResource();

	public string Description
		=> "PasteItemDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconPaste");

	public HotKey HotKey
		=> new(Keys.V, KeyModifiers.Ctrl);

	public bool IsExecutable
		=> GetIsExecutable();

	public PasteItemAction(IContentPageContext context)
    {
        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
        App.AppModel.PropertyChanged += AppModel_PropertyChanged;
	}

	public async Task ExecuteAsync(object? parameter = null)
	{
		if (context.ShellPage is null)
        {
            return;
        }

        var path = context.ShellPage.ShellViewModel.WorkingDirectory;
		await UIFilesystemHelpers.PasteItemAsync(path, context.ShellPage);
	}

	public bool GetIsExecutable()
	{
		return
            App.AppModel.IsPasteEnabled &&
			context.PageType != ContentPageTypes.Home &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.SearchResults;
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IContentPageContext.PageType))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }

	private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(AppModel.IsPasteEnabled))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
