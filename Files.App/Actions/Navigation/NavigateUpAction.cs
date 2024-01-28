﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class NavigateUpAction : ObservableObject, IAction
{
	private readonly IContentPageContext context;

	public string Label
		=> "Up".ToLocalized();

	public string Description
		=> "NavigateUpDescription".ToLocalized();

	public HotKey HotKey
		=> new(Keys.Up, KeyModifiers.Menu);

	public RichGlyph Glyph
		=> new("\uE74A");

	public bool IsExecutable
		=> context.CanNavigateToParent;

	public NavigateUpAction(IFolderViewViewModel folderViewViewModel)
    {
        context = folderViewViewModel.GetService<IContentPageContext>();

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
	{
		context.ShellPage!.Up_Click();

		return Task.CompletedTask;
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.CanNavigateToParent):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
