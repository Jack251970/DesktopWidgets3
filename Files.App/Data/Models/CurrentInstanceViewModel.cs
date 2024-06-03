// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Models;

public class CurrentInstanceViewModel : ObservableObject
{
    // FILESTODO:
    //  In the future, we should consolidate these public variables into
    //  a single enum property providing simplified customization of the
    //  values being manipulated inside the setter blocks

    public LayoutPreferencesManager FolderSettings { get; private set; } = null!;

    private FolderLayoutModes? RootLayoutMode { get; set; }

	public CurrentInstanceViewModel()
	{
        FolderSettings = new LayoutPreferencesManager();
	}

    public CurrentInstanceViewModel(FolderLayoutModes rootLayoutMode) : this()
    {
        RootLayoutMode = rootLayoutMode;
    }

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderSettings.Initialize(folderViewViewModel, RootLayoutMode);
    }

    private bool isPageTypeSearchResults = false;
	public bool IsPageTypeSearchResults
	{
		get => isPageTypeSearchResults;
		set
		{
			SetProperty(ref isPageTypeSearchResults, value);
			OnPropertyChanged(nameof(CanCreateFileInPage));
			OnPropertyChanged(nameof(CanCopyPathInPage));
		}
	}

	private string currentSearchQuery = null!;
	public string CurrentSearchQuery
	{
		get => currentSearchQuery;
		set => SetProperty(ref currentSearchQuery, value);
	}

    private bool isPageTypeNotHome = false;
	public bool IsPageTypeNotHome
	{
		get => isPageTypeNotHome;
		set
		{
			SetProperty(ref isPageTypeNotHome, value);
			OnPropertyChanged(nameof(CanCreateFileInPage));
			OnPropertyChanged(nameof(CanCopyPathInPage));
		}
	}

	private bool isPageTypeMtpDevice = false;
	public bool IsPageTypeMtpDevice
	{
		get => isPageTypeMtpDevice;
		set
		{
			SetProperty(ref isPageTypeMtpDevice, value);
			OnPropertyChanged(nameof(CanCreateFileInPage));
			OnPropertyChanged(nameof(CanCopyPathInPage));
		}
	}

	private bool isPageTypeRecycleBin = false;
	public bool IsPageTypeRecycleBin
	{
		get => isPageTypeRecycleBin;
		set
		{
			SetProperty(ref isPageTypeRecycleBin, value);
			OnPropertyChanged(nameof(CanCreateFileInPage));
			OnPropertyChanged(nameof(CanCopyPathInPage));
			OnPropertyChanged(nameof(CanTagFilesInPage));
		}
	}

	private bool isPageTypeFtp = false;
	public bool IsPageTypeFtp
	{
		get => isPageTypeFtp;
		set
		{
			SetProperty(ref isPageTypeFtp, value);
			OnPropertyChanged(nameof(CanCreateFileInPage));
			OnPropertyChanged(nameof(CanTagFilesInPage));
		}
	}

	private bool isPageTypeCloudDrive = false;
	public bool IsPageTypeCloudDrive
    {
        get => isPageTypeCloudDrive;
        set => SetProperty(ref isPageTypeCloudDrive, value);
    }

    private bool isPageTypeZipFolder = false;
	public bool IsPageTypeZipFolder
	{
		get => isPageTypeZipFolder;
		set
		{
			SetProperty(ref isPageTypeZipFolder, value);
			OnPropertyChanged(nameof(CanCreateFileInPage));
			OnPropertyChanged(nameof(CanTagFilesInPage));
		}
	}

	private bool isPageTypeLibrary = false;
	public bool IsPageTypeLibrary
    {
        get => isPageTypeLibrary;
        set => SetProperty(ref isPageTypeLibrary, value);
    }

    public bool CanCopyPathInPage => !isPageTypeMtpDevice && !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;

    public bool CanCreateFileInPage => !isPageTypeMtpDevice && !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults && !isPageTypeFtp && !isPageTypeZipFolder;

    public bool CanTagFilesInPage => !isPageTypeRecycleBin && !isPageTypeFtp && !isPageTypeZipFolder;

    public bool IsGitRepository => !string.IsNullOrWhiteSpace(gitRepositoryPath);

	private string? gitRepositoryPath;
	public string? GitRepositoryPath
	{
		get => gitRepositoryPath;
		set
		{
			if (SetProperty(ref gitRepositoryPath, value))
            {
                OnPropertyChanged(nameof(IsGitRepository));
            }
        }
	}

	private string gitBranchName = string.Empty;
	public string GitBranchName
	{
		get => gitBranchName;
		set => SetProperty(ref gitBranchName, value);
	}
}
