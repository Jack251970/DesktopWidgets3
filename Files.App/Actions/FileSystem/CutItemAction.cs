﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class CutItemAction : ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IContentPageContext context;

	public string Label
		=> "Cut".ToLocalized();

	public string Description
		=> "CutItemDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconCut");

	public HotKey HotKey
		=> new(Keys.X, KeyModifiers.Ctrl);

	public bool IsExecutable
		=> context.HasSelection;

	public CutItemAction(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;

        context = folderViewViewModel.GetService<IContentPageContext>();

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
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
