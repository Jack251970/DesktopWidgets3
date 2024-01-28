// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class CopyItemAction : ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IContentPageContext context;

	public string Label
		=> "Copy".ToLocalized();

	public string Description
		=> "CopyItemDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconCopy");

	public HotKey HotKey
		=> new(Keys.C, KeyModifiers.Ctrl);

	public bool IsExecutable
		=> context.HasSelection;

	public CopyItemAction(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;

        context = folderViewViewModel.GetService<IContentPageContext>();

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
	{
		if (context.ShellPage is not null)
        {
            return UIFilesystemHelpers.CopyItemAsync(FolderViewViewModel, context.ShellPage);
        }

        return Task.CompletedTask;
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IContentPageContext.HasSelection))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
