// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.


namespace Files.App.Actions;

internal class DeleteItemAction : BaseDeleteAction, IAction
{
    public string Label
		=> "Delete".ToLocalized();

	public string Description
		=> "DeleteItemDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconDelete");

	public HotKey HotKey
		=> new(Keys.Delete);

	public HotKey SecondHotKey
		=> new(Keys.D, KeyModifiers.Ctrl);

    public DeleteItemAction(IFolderViewViewModel folderViewViewModel) : base(folderViewViewModel)
    {
    }

    public Task ExecuteAsync()
	{
		return DeleteItemsAsync(false);
	}
}
