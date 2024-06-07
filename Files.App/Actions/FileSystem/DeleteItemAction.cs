﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class DeleteItemAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : BaseDeleteAction(folderViewViewModel, context), IAction
{
    public string Label
		=> "Delete".GetLocalizedResource();

	public string Description
		=> "DeleteItemDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconDelete");

	public HotKey HotKey
		=> new(Keys.Delete);

	public HotKey SecondHotKey
		=> new(Keys.D, KeyModifiers.Ctrl);

    public Task ExecuteAsync(object? parameter = null)
	{
		return DeleteItemsAsync(false);
	}
}
