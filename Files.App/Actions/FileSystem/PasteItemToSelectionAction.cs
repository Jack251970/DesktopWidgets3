// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class PasteItemToSelectionAction : BaseUIAction, IAction
{
    private readonly AppModel AppModel = DependencyExtensions.GetService<AppModel>();

    private readonly IContentPageContext context;

	public string Label
		=> "Paste".ToLocalized();

	public string Description
		=> "PasteItemToSelectionDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconPaste");

	public HotKey HotKey
		=> new(Keys.V, KeyModifiers.CtrlShift);

	public override bool IsExecutable
		=> GetIsExecutable();

	public PasteItemToSelectionAction(IFolderViewViewModel folderViewViewModel) : base(folderViewViewModel)
    {
		context = FolderViewViewModel.GetService<IContentPageContext>();

		context.PropertyChanged += Context_PropertyChanged;
		AppModel.PropertyChanged += AppModel_PropertyChanged;
	}

	public async Task ExecuteAsync()
	{
		if (context.ShellPage is null)
        {
            return;
        }

        var path = context.SelectedItem is ListedItem selectedItem
			? selectedItem.ItemPath
			: context.ShellPage.FilesystemViewModel.WorkingDirectory;

		await UIFilesystemHelpers.PasteItemAsync(path, context.ShellPage);
	}

	public bool GetIsExecutable()
	{
		if (!AppModel.IsPasteEnabled)
        {
            return false;
        }

        if (context.PageType is ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.SearchResults)
        {
            return false;
        }

        if (!context.HasSelection)
        {
            return true;
        }

        return
			context.SelectedItem?.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder &&
			FolderViewViewModel.CanShowDialog;
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.PageType):
			case nameof(IContentPageContext.SelectedItem):
				OnPropertyChanged(nameof(IsExecutable));
				break;
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
