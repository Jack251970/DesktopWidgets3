// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class FormatDriveAction : ObservableObject, IAction
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

	public FormatDriveAction(IContentPageContext context)
    {
        this.context = context;
        drivesViewModel = DependencyExtensions.GetRequiredService<DrivesViewModel>();

		context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
        return Win32Helper.OpenFormatDriveDialog(context.Folder?.ItemPath ?? string.Empty);
    }

    public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IContentPageContext.HasItem))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
