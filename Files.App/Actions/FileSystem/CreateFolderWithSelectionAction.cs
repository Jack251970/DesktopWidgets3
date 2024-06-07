﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class CreateFolderWithSelectionAction : ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IContentPageContext context;

	public string Label
		=> "CreateFolderWithSelection".GetLocalizedResource();

	public string Description
		=> "CreateFolderWithSelectionDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconNewFolder");

	public bool IsExecutable =>
		context.ShellPage is not null &&
		context.HasSelection;

	public CreateFolderWithSelectionAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context)
    {
        FolderViewViewModel = folderViewViewModel;

        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
		return UIFilesystemHelpers.CreateFolderWithSelectionAsync(FolderViewViewModel, context.ShellPage!);
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.ShellPage):
			case nameof(IContentPageContext.HasSelection):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
