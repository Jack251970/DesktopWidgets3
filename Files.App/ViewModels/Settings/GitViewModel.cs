// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.ViewModels.Settings;

public class GitViewModel : ObservableObject
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    protected readonly IFileTagsSettingsService FileTagsSettingsService = DependencyExtensions.GetService<IFileTagsSettingsService>();

	public ICommand RemoveCredentialsCommand { get; }
	public ICommand ConnectToGitHubCommand { get; }

	// Enabled when there are saved credentials
	private bool _IsLogoutEnabled;
	public bool IsLogoutEnabled
	{
		get => _IsLogoutEnabled;
		set => SetProperty(ref _IsLogoutEnabled, value);
	}

	public GitViewModel()
	{
		RemoveCredentialsCommand = new RelayCommand(DoRemoveCredentials);
		ConnectToGitHubCommand = new RelayCommand(DoConnectToGitHubAsync);

		IsLogoutEnabled = GitHelpers.GetSavedCredentials() != string.Empty;
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
    }

    public void DoRemoveCredentials()
	{
		GitHelpers.RemoveSavedCredentials();
		IsLogoutEnabled = false;
	}
		
	public async void DoConnectToGitHubAsync()
	{
		UIHelpers.CloseAllDialogs(FolderViewViewModel);
		await GitHelpers.RequireGitAuthenticationAsync(FolderViewViewModel);
	}
}
