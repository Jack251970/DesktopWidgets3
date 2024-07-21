// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal abstract class BaseOpenInNewPaneAction : ObservableObject, IAction
{
    protected IFolderViewViewModel FolderViewViewModel { get; private set; }

	protected IUserSettingsService UserSettingsService { get; private set; }
	protected IContentPageContext ContentPageContext { get; private set; }
    protected IHomePageContext HomePageContext { get; private set; }
    protected ISidebarContext SidebarContext { get; } = DependencyExtensions.GetRequiredService<ISidebarContext>();

    public string Label
		=> "OpenInNewPane".GetLocalizedResource();

	public string Description
		=> "OpenDirectoryInNewPaneDescription".GetLocalizedResource();

	public virtual bool IsExecutable =>
		ContentPageContext.SelectedItem is not null &&
		ContentPageContext.SelectedItem.IsFolder &&
		UserSettingsService.GeneralSettingsService.ShowOpenInNewPane;

	public virtual bool IsAccessibleGlobally
		=> true;

	public BaseOpenInNewPaneAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context)
    {
        FolderViewViewModel = folderViewViewModel;

        ContentPageContext = context;
        UserSettingsService = folderViewViewModel.GetRequiredService<IUserSettingsService>();
        HomePageContext = folderViewViewModel.GetRequiredService<IHomePageContext>();

		ContentPageContext.PropertyChanged += Context_PropertyChanged;
	}

	public virtual Task ExecuteAsync(object? parameter = null)
	{
		NavigationHelpers.OpenInSecondaryPane(
			ContentPageContext.ShellPage!,
			ContentPageContext.ShellPage!.SlimContentPage.SelectedItems!.FirstOrDefault()!);

		return Task.CompletedTask;
	}

	protected virtual void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.ShellPage):
			case nameof(IContentPageContext.PageType):
			case nameof(IContentPageContext.HasSelection):
			case nameof(IContentPageContext.SelectedItems):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
