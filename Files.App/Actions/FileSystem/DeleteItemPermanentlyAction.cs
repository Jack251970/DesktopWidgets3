// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.


namespace Files.App.Actions;

internal class DeleteItemPermanentlyAction : BaseDeleteAction, IAction
{
    public string Label
		=> "DeletePermanently".ToLocalized();

	public string Description
		=> "DeleteItemPermanentlyDescription".ToLocalized();

	public HotKey HotKey
		=> new(Keys.Delete, KeyModifiers.Shift);

    public DeleteItemPermanentlyAction(IFolderViewViewModel folderViewViewModel) : base(folderViewViewModel)
    {
    }

    public Task ExecuteAsync()
	{
		return DeleteItemsAsync(true);
	}
}
