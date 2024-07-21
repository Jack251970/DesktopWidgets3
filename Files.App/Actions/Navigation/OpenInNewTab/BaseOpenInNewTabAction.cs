// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal abstract class BaseOpenInNewTabAction : ObservableObject, IAction
{
    protected IFolderViewViewModel FolderViewViewModel { get; private set; }

	protected IUserSettingsService UserSettingsService { get; private set; }
	protected IContentPageContext ContentPageContext { get; private set; }
	protected IHomePageContext HomePageContext { get; private set; }
	protected ISidebarContext SidebarContext { get; } = DependencyExtensions.GetRequiredService<ISidebarContext>();

	public string Label
		=> "OpenInNewTab".GetLocalizedResource();

	public string Description
		=> "OpenDirectoryInNewTabDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconOpenInNewTab");

	public virtual bool IsAccessibleGlobally
		=> true;

	public virtual bool IsExecutable =>
		ContentPageContext.ShellPage is not null &&
		ContentPageContext.ShellPage.SlimContentPage is not null &&
		ContentPageContext.SelectedItems.Count is not 0 &&
		ContentPageContext.SelectedItems.Count <= 5 &&
		ContentPageContext.SelectedItems.Count(x => x.IsFolder) == ContentPageContext.SelectedItems.Count &&
		UserSettingsService.GeneralSettingsService.ShowOpenInNewTab;

	public BaseOpenInNewTabAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context)
    {
        FolderViewViewModel = folderViewViewModel;

        ContentPageContext = context;
        UserSettingsService = folderViewViewModel.GetRequiredService<IUserSettingsService>();
        HomePageContext = folderViewViewModel.GetRequiredService<IHomePageContext>();

		ContentPageContext.PropertyChanged += Context_PropertyChanged;
	}

	public async virtual Task ExecuteAsync(object? parameter = null)
	{
		foreach (var listedItem in ContentPageContext.SelectedItems)
		{
            // CHANGE: Opening in explorer instead of opening in new tab.
            await NavigationHelpers.OpenInExplorerAsync((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
            /*await ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				await NavigationHelpers.AddNewTabByPathAsync(
                    FolderViewViewModel,
					typeof(ShellPanesPage),
					(listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath,
					false);
			},
			Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);*/
		}
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
