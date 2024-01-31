// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class DeleteItemPermanentlyAction : BaseDeleteAction, IAction
{
    public string Label
		=> "DeletePermanently".GetLocalizedResource();

	public string Description
		=> "DeleteItemPermanentlyDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.Delete, KeyModifiers.Shift);

    public DeleteItemPermanentlyAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(folderViewViewModel, context)
    {
    }

    public Task ExecuteAsync()
	{
		return DeleteItemsAsync(true);
	}
}
