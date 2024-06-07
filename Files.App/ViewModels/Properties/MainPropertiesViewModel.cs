﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views.Properties;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Files.App.ViewModels.Properties;

public sealed class MainPropertiesViewModel : ObservableObject
{
	public CancellationTokenSource ChangedPropertiesCancellationTokenSource { get; }

	public ObservableCollection<NavigationViewItemButtonStyleItem> NavigationViewItems { get; }

	private NavigationViewItemButtonStyleItem _SelectedNavigationViewItem = null!;
	public NavigationViewItemButtonStyleItem SelectedNavigationViewItem
	{
		get => _SelectedNavigationViewItem;
		set
		{
            if (SetProperty(ref _SelectedNavigationViewItem, value))
            {
                var parameter = new PropertiesPageNavigationParameter
				{
                    FolderViewViewModel = FolderViewViewModel,
					AppInstance = _parameter.AppInstance,
					CancellationTokenSource = ChangedPropertiesCancellationTokenSource,
					Parameter = _parameter.Parameter,
					Window = Window
				};

				var page = value.ItemType switch
				{
					PropertiesNavigationViewItemType.General =>       typeof(GeneralPage),
					PropertiesNavigationViewItemType.Shortcut =>      typeof(ShortcutPage),
					PropertiesNavigationViewItemType.Library =>       typeof(LibraryPage),
					PropertiesNavigationViewItemType.Details =>       typeof(DetailsPage),
					PropertiesNavigationViewItemType.Security =>      typeof(SecurityPage),
					PropertiesNavigationViewItemType.Customization => typeof(CustomizationPage),
					PropertiesNavigationViewItemType.Compatibility => typeof(CompatibilityPage),
					PropertiesNavigationViewItemType.Hashes =>        typeof(HashesPage),
					_ => typeof(GeneralPage),
				};

				_mainFrame?.Navigate(page, parameter, new EntranceNavigationTransitionInfo());
			}
		}
	}

    //public string TitleBarText
    //{
    //	get
    //	{
    //		// Library
    //		if (_baseProperties is LibraryProperties library)
    //			return library.Library.Name;
    //		// Drive
    //		else if (_baseProperties is DriveProperties drive)
    //			return drive.Drive.Text;
    //		// Storage objects (multi-selected)
    //		else if (_baseProperties is CombinedProperties combined)
    //			return string.Join(", ", combined.List.Select(x => x.Name));
    //		// File
    //		else if (_baseProperties is FileProperties file)
    //			return file.Item.Name;
    //		// Folder
    //		else if (_baseProperties is FolderProperties folder)
    //			return folder.Item.Name;
    //		else
    //			return string.Empty;
    //	}
    //}

    private readonly IFolderViewViewModel FolderViewViewModel;

	private readonly Window Window;

	private AppWindow AppWindow => Window.AppWindow;

	private readonly Frame _mainFrame;

    private readonly BaseProperties _baseProperties;

    private readonly PropertiesPageNavigationParameter _parameter;

    public IRelayCommand DoBackwardNavigationCommand { get; }
	public IAsyncRelayCommand SaveChangedPropertiesCommand { get; }
	public IRelayCommand CancelChangedPropertiesCommand { get; }

	public MainPropertiesViewModel(IFolderViewViewModel folderViewViewModel, Window window, Frame mainFrame, BaseProperties baseProperties, PropertiesPageNavigationParameter parameter)
	{
		ChangedPropertiesCancellationTokenSource = new();

        FolderViewViewModel = folderViewViewModel;
		Window = window;
		_mainFrame = mainFrame;
		_parameter = parameter;
		_baseProperties = baseProperties;

		DoBackwardNavigationCommand = new RelayCommand(ExecuteDoBackwardNavigationCommand);
		SaveChangedPropertiesCommand = new AsyncRelayCommand(ExecuteSaveChangedPropertiesCommandAsync);
		CancelChangedPropertiesCommand = new RelayCommand(ExecuteCancelChangedPropertiesCommand);

		NavigationViewItems = PropertiesNavigationViewItemFactory.Initialize(parameter.Parameter);
		SelectedNavigationViewItem = NavigationViewItems.First(x => x.ItemType == PropertiesNavigationViewItemType.General);
	}

	private void ExecuteDoBackwardNavigationCommand()
	{
		if (NavigationViewItems is null ||
			NavigationViewItems.Count == 0 ||
			_mainFrame is null)
        {
            return;
        }

        if (_mainFrame.CanGoBack)
        {
            _mainFrame.GoBack();
        }

        var pageTag = ((Page)_mainFrame.Content).Tag.ToString();

		// Move selection indicator
		_SelectedNavigationViewItem =
			NavigationViewItems.First(x => string.Equals(x.ItemType.ToString(), pageTag, StringComparison.CurrentCultureIgnoreCase))
			?? NavigationViewItems.First();
		OnPropertyChanged(nameof(SelectedNavigationViewItem));
	}

	private async Task ExecuteSaveChangedPropertiesCommandAsync()
	{
		await ApplyChangesAsync();
        await WindowsExtensions.CloseWindow(Window);
    }

	private async void ExecuteCancelChangedPropertiesCommand()
	{
        await WindowsExtensions.CloseWindow(Window);
    }

	private async Task ApplyChangesAsync()
	{
		if (_mainFrame is not null && _mainFrame.Content is not null)
        {
            await ((BasePropertiesPage)_mainFrame.Content).SaveChangesAsync();
        }
    }
}
