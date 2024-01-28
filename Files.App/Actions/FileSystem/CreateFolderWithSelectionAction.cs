// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class CreateFolderWithSelectionAction : ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IContentPageContext context;

	public string Label
		=> "CreateFolderWithSelection".ToLocalized();

	public string Description
		=> "CreateFolderWithSelectionDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconNewFolder");

	public bool IsExecutable =>
		context.ShellPage is not null &&
		context.HasSelection;

	public CreateFolderWithSelectionAction(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;

        context = folderViewViewModel.GetService<IContentPageContext>();

        context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
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
