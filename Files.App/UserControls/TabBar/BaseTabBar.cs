// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabBar;

/// <summary>
/// Represents base class for <see cref="TabBar"/>.
/// </summary>
public abstract class BaseTabBar : ITabBar
{
    public Action<object, RoutedEventArgs>? Loaded;

    protected IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

	protected ITabBarItemContent CurrentSelectedAppInstance = null!;

    // CHANGE: Non-static event handler instead of static one.
    public event EventHandler<ITabBar>? OnLoaded;

    // CHANGE: Non-static event handler instead of static one.
    public event PropertyChangedEventHandler? StaticPropertyChanged;

    public const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";

	public const string TabPathIdentifier = "FilesTabViewItemPath";

	// RecentlyClosedTabs is shared between all multitasking controls
	public static Stack<CustomTabViewItemParameter[]> RecentlyClosedTabs { get; private set; } = new();

    public ObservableCollection<TabBarItem> Items
		=> MainPageViewModel.AppInstances[FolderViewViewModel];

	public event EventHandler<CurrentInstanceChangedEventArgs>? CurrentInstanceChanged;

    // CHANGE: Non-static property instead of static one.
    private bool _IsRestoringClosedTab;
	public bool IsRestoringClosedTab
	{
		get => _IsRestoringClosedTab;
		private set
		{
			_IsRestoringClosedTab = value;
			StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(IsRestoringClosedTab)));
		}
	}

	public BaseTabBar()
	{
		Loaded += TabView_Loaded;
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;

        // CHANGE: Use property changed to handle tab view selection changes.
        FolderViewViewModel.PropertyChanged += FolderViewViewModel_PropertyChanged;

        // CHANGE: Use loaded event to handle tab view loaded event.
        Loaded?.Invoke(this, null!);
    }

    private void FolderViewViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(FolderViewViewModel.TabStripSelectedIndex):
                TabView_SelectionChanged(this, null!);
                break;
        }
    }

    public virtual DependencyObject ContainerFromItem(ITabBarItem item)
	{
		return null!;
	}

	private void TabView_CurrentInstanceChanged(object? sender, CurrentInstanceChangedEventArgs e)
	{
		foreach (var instance in e.PageInstances)
		{
			if (instance is not null)
			{
				instance.IsCurrentInstance = instance == e.CurrentInstance;
			}
		}
	}

    protected void TabView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (FolderViewViewModel.TabStripSelectedIndex >= 0 && FolderViewViewModel.TabStripSelectedIndex < Items.Count)
		{
			CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();

			if (CurrentSelectedAppInstance is not null)
			{
				CurrentInstanceChanged?.Invoke(this, new CurrentInstanceChangedEventArgs()
				{
					CurrentInstance = CurrentSelectedAppInstance,
					PageInstances = GetAllTabInstances()
				});
			}
		}
	}

	protected void TabView_TabCloseRequested(TabView _, TabViewTabCloseRequestedEventArgs args)
	{
		CloseTab((TabBarItem)args.Item);
	}

    protected void OnCurrentInstanceChanged(CurrentInstanceChangedEventArgs args)
	{
		CurrentInstanceChanged?.Invoke(this, args);
	}

	public void TabView_Loaded(object sender, RoutedEventArgs e)
	{
		CurrentInstanceChanged += TabView_CurrentInstanceChanged;
		OnLoaded?.Invoke(null, this);
	}

	public ITabBarItemContent GetCurrentSelectedTabInstance()
	{
		return MainPageViewModel.AppInstances[FolderViewViewModel][FolderViewViewModel.TabStripSelectedIndex].TabItemContent;
	}

    public void SelectionChanged()
	{
		TabView_SelectionChanged(null!, null!);
	}

    // CHANGE: Non-static function instead of static one.
    public void PushRecentTab(CustomTabViewItemParameter[] tab)
	{
		RecentlyClosedTabs.Push(tab);
		StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(RecentlyClosedTabs)));
	}

    public List<ITabBarItemContent> GetAllTabInstances()
	{
		return MainPageViewModel.AppInstances[FolderViewViewModel].Select(x => x.TabItemContent).ToList();
	}

    public async Task ReopenClosedTabAsync()
	{
		if (!IsRestoringClosedTab && RecentlyClosedTabs.Count > 0)
		{
			IsRestoringClosedTab = true;
			var lastTab = RecentlyClosedTabs.Pop();
			foreach (var item in lastTab)
            {
                await NavigationHelpers.AddNewTabByParamAsync(FolderViewViewModel, item.InitialPageType, item.NavigationParameter);
            }

            IsRestoringClosedTab = false;
		}
	}

    // CHANGE: Remove MoveTabToNewWindowAsync method.
    /*public async void MoveTabToNewWindowAsync(object sender, RoutedEventArgs e)
	{
		await MultitaskingTabsHelpers.MoveTabToNewWindow(((FrameworkElement)sender).DataContext as TabBarItem, this);
	}*/

    public async void CloseTab(TabBarItem tabItem)
    {
        if (tabItem is null)
        {
            return;
        }

        Items.Remove(tabItem);
        tabItem.Unload();

        // Dispose and save tab arguments
        PushRecentTab(
        [
            tabItem.NavigationParameter,
        ]);

        // Save the updated tab list
        AppLifecycleHelper.SaveSessionTabs(FolderViewViewModel);

        if (Items.Count == 0)
        {
            await WindowsExtensions.CloseWindow(FolderViewViewModel.MainWindow);
        }
    }

    public void SetLoadingIndicatorStatus(ITabBarItem item, bool loading)
	{
		if (ContainerFromItem(item) is not Control tabItem)
        {
            return;
        }

        var stateToGoName = loading ? "Loading" : "NotLoading";

		VisualStateManager.GoToState(tabItem, stateToGoName, false);
	}
}
