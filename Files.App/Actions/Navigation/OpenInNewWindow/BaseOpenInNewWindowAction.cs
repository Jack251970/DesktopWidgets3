// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Actions;

internal abstract class BaseOpenInNewWindowAction : ObservableObject, IAction
{
    protected IFolderViewViewModel FolderViewViewModel { get; private set; }

	protected IUserSettingsService UserSettingsService { get; private set; }
	protected IContentPageContext ContentPageContext { get; private set; }
	protected IHomePageContext HomePageContext { get; private set; }
	protected ISidebarContext SidebarContext { get; } = DependencyExtensions.GetRequiredService<ISidebarContext>();

	public string Label
		=> "OpenInNewWindow".GetLocalizedResource();

	public string Description
		=> "OpenInNewWindowDescription".GetLocalizedResource();

	public virtual HotKey HotKey
		=> new(Keys.Enter, KeyModifiers.CtrlAlt);

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconOpenInNewWindow");

	public virtual bool IsAccessibleGlobally
		=> true;

	public virtual bool IsExecutable =>
		ContentPageContext.ShellPage is not null &&
		ContentPageContext.ShellPage.SlimContentPage is not null &&
		ContentPageContext.SelectedItems.Count is not 0 &&
		ContentPageContext.SelectedItems.Count <= 5 &&
		ContentPageContext.SelectedItems.Count(x => x.IsFolder) == ContentPageContext.SelectedItems.Count &&
		UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow;

	public BaseOpenInNewWindowAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context)
    {
        FolderViewViewModel = folderViewViewModel;

        ContentPageContext = context;
        UserSettingsService = folderViewViewModel.GetRequiredService<IUserSettingsService>();
        HomePageContext = folderViewViewModel.GetRequiredService<IHomePageContext>();

		ContentPageContext.PropertyChanged += Context_PropertyChanged;
	}

	public async virtual Task ExecuteAsync(object? parameter = null)
	{
		if (ContentPageContext.ShellPage?.SlimContentPage?.SelectedItems is null)
        {
            return;
        }

        var items = ContentPageContext.ShellPage.SlimContentPage.SelectedItems;

		foreach (var listedItem in items)
		{
            // CHANGE: Opening in explorer instead of opening in new window.
            var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
            await NavigationHelpers.OpenInExplorerAsync(selectedItemPath);
            /*var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");

			await Launcher.LaunchUriAsync(folderUri);*/
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
