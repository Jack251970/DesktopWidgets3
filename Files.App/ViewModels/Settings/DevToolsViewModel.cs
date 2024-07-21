// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.ViewModels.Settings;

public sealed class DevToolsViewModel : ObservableObject
{
    private IFolderViewViewModel FolderViewViewModel { get; set; }

    public readonly IFileTagsSettingsService FileTagsSettingsService = DependencyExtensions.GetRequiredService<IFileTagsSettingsService>();
    protected IDevToolsSettingsService DevToolsSettingsService { get; private set; }

    public Dictionary<OpenInIDEOption, string> OpenInIDEOptions { get; private set; } = [];

    public ICommand RemoveCredentialsCommand { get; }
	public ICommand ConnectToGitHubCommand { get; }

	// Enabled when there are saved credentials
	private bool _IsLogoutEnabled;
	public bool IsLogoutEnabled
	{
		get => _IsLogoutEnabled;
		set => SetProperty(ref _IsLogoutEnabled, value);
	}

	public DevToolsViewModel()
	{
        /*// Open in IDE options
        OpenInIDEOptions.Add(OpenInIDEOption.GitRepos, "GitRepos".GetLocalizedResource());
        OpenInIDEOptions.Add(OpenInIDEOption.AllLocations, "AllLocations".GetLocalizedResource());
        SelectedOpenInIDEOption = OpenInIDEOptions[DevToolsSettingsService.OpenInIDEOption];*/

        IsLogoutEnabled = GitHelpers.GetSavedCredentials() != string.Empty;

        RemoveCredentialsCommand = new RelayCommand(DoRemoveCredentials);
		ConnectToGitHubCommand = new RelayCommand(DoConnectToGitHubAsync);
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
        DevToolsSettingsService = folderViewViewModel.GetRequiredService<IDevToolsSettingsService>();

        // Open in IDE options
        OpenInIDEOptions.Add(OpenInIDEOption.GitRepos, "GitRepos".GetLocalizedResource());
        OpenInIDEOptions.Add(OpenInIDEOption.AllLocations, "AllLocations".GetLocalizedResource());
        SelectedOpenInIDEOption = OpenInIDEOptions[DevToolsSettingsService.OpenInIDEOption];
    }

    private string selectedOpenInIDEOption;
    public string SelectedOpenInIDEOption
    {
        get => selectedOpenInIDEOption;
        set
        {
            if (SetProperty(ref selectedOpenInIDEOption, value))
            {
                DevToolsSettingsService.OpenInIDEOption = OpenInIDEOptions.First(e => e.Value == value).Key;
            }
        }
    }

    public void DoRemoveCredentials()
	{
		GitHelpers.RemoveSavedCredentials();
		IsLogoutEnabled = false;
	}
		
	public async void DoConnectToGitHubAsync()
	{
		UIHelpers.CloseAllDialogs(FolderViewViewModel);

        await Task.Delay(500);

        await GitHelpers.RequireGitAuthenticationAsync(FolderViewViewModel);
	}
}
