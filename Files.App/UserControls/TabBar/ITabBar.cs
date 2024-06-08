// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.UserControls.TabBar;

/// <summary>
/// Represents an interface for <see cref="UserControls.TabBar"/>.
/// </summary>
public interface ITabBar
{
	public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

	public ObservableCollection<TabBarItem> Items { get; }

	public ITabBarItemContent GetCurrentSelectedTabInstance();

    // CHANGE: Add push recent tab interface.
    public void PushRecentTab(CustomTabViewItemParameter[] tab);

    public List<ITabBarItemContent> GetAllTabInstances();

    public Task ReopenClosedTabAsync();

	public void CloseTab(TabBarItem tabItem);

    public void SetLoadingIndicatorStatus(ITabBarItem item, bool loading);
}
