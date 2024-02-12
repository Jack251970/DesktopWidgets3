﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts;

public interface IMultitaskingContext : INotifyPropertyChanged
{
    void Initialize(IFolderViewViewModel folderViewViewModel);

	ITabBar? Control { get; }

	ushort TabCount { get; }

	TabBarItem CurrentTabItem { get; }
	ushort CurrentTabIndex { get; }

	TabBarItem SelectedTabItem { get; }
	ushort SelectedTabIndex { get; }
}
