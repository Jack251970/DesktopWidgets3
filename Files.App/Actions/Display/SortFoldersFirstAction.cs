﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class SortFoldersFirstAction : ObservableObject, IToggleAction
{
	private readonly IDisplayPageContext context;

	public string Label
		=> "SortFoldersFirst".GetLocalizedResource();

	public string Description
		=> "SortFoldersFirstDescription".GetLocalizedResource();

	public bool IsOn
		=> !context.SortFilesFirst && !context.SortDirectoriesAlongsideFiles;

	public SortFoldersFirstAction(IDisplayPageContext context)
    {
        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
    }

    public Task ExecuteAsync(object? parameter = null)
	{
		context.SortFilesFirst = false;
		context.SortDirectoriesAlongsideFiles = false;

		return Task.CompletedTask;
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IDisplayPageContext.SortFilesFirst) or nameof(IDisplayPageContext.SortDirectoriesAlongsideFiles))
        {
            OnPropertyChanged(nameof(IsOn));
        }
    }
}
