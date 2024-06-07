// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;

namespace Files.App.Data.Contexts;

internal sealed class MultitaskingContext : ObservableObject, IMultitaskingContext
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

	private readonly bool isPopupOpen = false;

	private readonly ITabBar? control = null;
	public ITabBar? Control => control;

	private readonly ushort tabCount = 0;
	public ushort TabCount => tabCount;

	public TabBarItem CurrentTabItem => MainPageViewModel.AppInstances[FolderViewViewModel].ElementAtOrDefault(currentTabIndex)!;
	public TabBarItem SelectedTabItem => MainPageViewModel.AppInstances[FolderViewViewModel].ElementAtOrDefault(selectedTabIndex)!;

	private ushort currentTabIndex = 0;
	public ushort CurrentTabIndex => currentTabIndex;

	private ushort selectedTabIndex = 0;
	public ushort SelectedTabIndex => selectedTabIndex;

	public MultitaskingContext()
	{
		/*MainPageViewModel.AppInstances.CollectionChanged += AppInstances_CollectionChanged;
		App.AppModel.PropertyChanged += AppModel_PropertyChanged;
		BaseTabBar.OnLoaded += BaseMultitaskingControl_OnLoaded;
		TabBar.SelectedTabItemChanged += HorizontalMultitaskingControl_SelectedTabItemChanged;
		FocusManager.GotFocus += FocusManager_GotFocus;
		FocusManager.LosingFocus += FocusManager_LosingFocus;*/
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
        // CHANGE: Remove events related to app instances.
        /*MainPageViewModel.AppInstances.CollectionChanged += AppInstances_CollectionChanged;*/
        FolderViewViewModel.PropertyChanged += AppModel_PropertyChanged;
        // CHANGE: Remove events related to tab bar.
        /*BaseTabBar.OnLoaded += BaseMultitaskingControl_OnLoaded;
        TabBar.SelectedTabItemChanged += HorizontalMultitaskingControl_SelectedTabItemChanged;*/
        FocusManager.GotFocus += FocusManager_GotFocus;
        FocusManager.LosingFocus += FocusManager_LosingFocus;
    }

    // CHANGE: Remove events related to app instances.
    /*private void AppInstances_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		UpdateTabCount();
	}*/

    private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(FolderViewViewModel.TabStripSelectedIndex))
        {
            UpdateCurrentTabIndex();
        }
    }

    // CHANGE: Remove events related to tab bar.
    /*private void BaseMultitaskingControl_OnLoaded(object? sender, ITabBar control)
	{
		SetProperty(ref this.control, control, nameof(Control));
		UpdateTabCount();
		UpdateCurrentTabIndex();
	}

	private void HorizontalMultitaskingControl_SelectedTabItemChanged(object? sender, TabBarItem? e)
	{
		isPopupOpen = e is not null;
		var newSelectedIndex = e is null ? currentTabIndex : MainPageViewModel.AppInstances[FolderViewViewModel].IndexOf(e);
		UpdateSelectedTabIndex(newSelectedIndex);
	}*/

    private void FocusManager_GotFocus(object? sender, FocusManagerGotFocusEventArgs e)
	{
		if (isPopupOpen)
        {
            return;
        }

        if (e.NewFocusedElement is FrameworkElement element && element.DataContext is TabBarItem tabItem)
		{
			var newSelectedIndex = MainPageViewModel.AppInstances[FolderViewViewModel].IndexOf(tabItem);
			UpdateSelectedTabIndex(newSelectedIndex);
		}
	}

	private void FocusManager_LosingFocus(object? sender, LosingFocusEventArgs e)
	{
		if (isPopupOpen)
        {
            return;
        }

        if (SetProperty(ref selectedTabIndex, currentTabIndex, nameof(SelectedTabIndex)))
		{
			OnPropertyChanged(nameof(selectedTabIndex));
		}
	}

	/*private void UpdateTabCount()
	{
		SetProperty(ref tabCount, (ushort)MainPageViewModel.AppInstances.Count, nameof(TabCount));
	}*/

	private void UpdateCurrentTabIndex()
	{
		if (SetProperty(ref currentTabIndex, (ushort)FolderViewViewModel.TabStripSelectedIndex, nameof(CurrentTabIndex)))
		{
			OnPropertyChanged(nameof(CurrentTabItem));
		}
	}

	private void UpdateSelectedTabIndex(int index)
	{
		if (SetProperty(ref selectedTabIndex, (ushort)index, nameof(SelectedTabIndex)))
		{
			OnPropertyChanged(nameof(SelectedTabItem));
		}
	}
}
