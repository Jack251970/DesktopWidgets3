// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Actions;

internal class OpenFileLocationAction : ObservableObject, IAction
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IContentPageContext context;

	public string Label
		=> "OpenFileLocation".ToLocalized();

	public string Description
		=> "OpenFileLocationDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(baseGlyph: "\uE8DA");

	public bool IsExecutable =>
		context.ShellPage is not null &&
		context.HasSelection &&
		context.SelectedItem is ShortcutItem;

	public OpenFileLocationAction(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;

        context = folderViewViewModel.GetService<IContentPageContext>();

        context.PropertyChanged += Context_PropertyChanged;
	}

	public async Task ExecuteAsync()
	{
		if (context.ShellPage?.FilesystemViewModel is null)
        {
            return;
        }

        var item = context.SelectedItem as ShortcutItem;

		if (string.IsNullOrWhiteSpace(item?.TargetPath))
        {
            return;
        }

        // Check if destination path exists
        var folderPath = Path.GetDirectoryName(item.TargetPath);
		var destFolder = await context.ShellPage.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath!);

		if (destFolder)
		{
			context.ShellPage?.NavigateWithArguments(context.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(folderPath!), new NavigationArguments()
			{
                FolderViewViewModel = FolderViewViewModel,
				NavPathParam = folderPath,
				SelectItems = new[] { Path.GetFileName(item.TargetPath.TrimPath())! },
				AssociatedTabInstance = context.ShellPage
			});
		}
		else if (destFolder == FileSystemStatusCode.NotFound)
		{
			await DialogDisplayHelper.ShowDialogAsync(FolderViewViewModel, "FileNotFoundDialog/Title".ToLocalized(), "FileNotFoundDialog/Text".ToLocalized());
		}
		else
		{
			await DialogDisplayHelper.ShowDialogAsync(FolderViewViewModel, "InvalidItemDialogTitle".ToLocalized(),
				string.Format("InvalidItemDialogContent".ToLocalized(), Environment.NewLine, destFolder.ErrorCode.ToString()));
		}
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IContentPageContext.HasSelection))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
