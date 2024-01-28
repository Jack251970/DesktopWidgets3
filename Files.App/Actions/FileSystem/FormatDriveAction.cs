// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class FormatDriveAction : ObservableObject, IAction
{
	private readonly IContentPageContext context;

	private readonly DrivesViewModel drivesViewModel;

	public string Label
		=> "FormatDriveText".GetLocalizedResource();

	public string Description
		=> "FormatDriveDescription".GetLocalizedResource();

	public bool IsExecutable =>
		context.HasItem &&
		!context.HasSelection &&
		(drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
			string.Equals(x.Path, context.Folder?.ItemPath))?.MenuOptions.ShowFormatDrive ?? false);

	public FormatDriveAction(IFolderViewViewModel folderViewViewModel)
    {
        context = folderViewViewModel.GetService<IContentPageContext>();
        drivesViewModel = DependencyExtensions.GetService<DrivesViewModel>();

		context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
	{
		return Win32API.OpenFormatDriveDialog(context.Folder?.ItemPath ?? string.Empty);
	}

	public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IContentPageContext.HasItem))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
