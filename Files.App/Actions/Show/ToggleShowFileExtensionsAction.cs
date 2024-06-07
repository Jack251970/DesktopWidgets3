// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class ToggleShowFileExtensionsAction : ObservableObject, IToggleAction
{
	private readonly IFoldersSettingsService settings;

	public string Label
		=> "ShowFileExtensions".GetLocalizedResource();

	public string Description
		=> "ToggleShowFileExtensionsDescription".GetLocalizedResource();

	public bool IsOn
		=> settings.ShowFileExtensions;

	public ToggleShowFileExtensionsAction(IFolderViewViewModel folderViewViewModel)
    {
		settings = folderViewViewModel.GetService<IFoldersSettingsService>();

		settings.PropertyChanged += Settings_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
		settings.ShowFileExtensions = !settings.ShowFileExtensions;

		return Task.CompletedTask;
	}

	private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IFoldersSettingsService.ShowFileExtensions))
        {
            OnPropertyChanged(nameof(IsOn));
        }
    }
}
