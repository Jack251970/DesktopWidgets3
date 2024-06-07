﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class CutItemAction : ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IContentPageContext context;

	public string Label
		=> "Cut".GetLocalizedResource();

	public string Description
		=> "CutItemDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconCut");

	public HotKey HotKey
		=> new(Keys.X, KeyModifiers.Ctrl);

	public bool IsExecutable
		=> context.HasSelection;

	public CutItemAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context)
    {
        FolderViewViewModel = folderViewViewModel;

        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
		return context.ShellPage is not null
			? UIFilesystemHelpers.CutItemAsync(FolderViewViewModel, context.ShellPage)
			: Task.CompletedTask;
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IContentPageContext.HasSelection))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
