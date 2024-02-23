// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using FocusManager = Microsoft.UI.Xaml.Input.FocusManager;

namespace Files.App.UserControls;

public sealed partial class AddressToolbar : UserControl
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    private IUserSettingsService UserSettingsService { get; set; } = null!;

    // Using a DependencyProperty as the backing store for Commands.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CommandsProperty =
        DependencyProperty.Register(nameof(Commands), typeof(ICommandManager), typeof(AddressToolbar), new(null));
    public ICommandManager Commands
    {
        get => (ICommandManager)GetValue(CommandsProperty);
        set => SetValue(CommandsProperty, value);
    }

    // Using a DependencyProperty as the backing store for ShowOngoingTasks.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowOngoingTasksProperty =
		DependencyProperty.Register(nameof(ShowOngoingTasks), typeof(bool), typeof(AddressToolbar), new(null));
	public bool ShowOngoingTasks
	{
		get => (bool)GetValue(ShowOngoingTasksProperty);
		set => SetValue(ShowOngoingTasksProperty, value);
	}

	// Using a DependencyProperty as the backing store for ShowSettingsButton.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty ShowSettingsButtonProperty =
		DependencyProperty.Register(nameof(ShowSettingsButton), typeof(bool), typeof(AddressToolbar), new(null));
	public bool ShowSettingsButton
	{
		get => (bool)GetValue(dp: ShowSettingsButtonProperty);
		set => SetValue(ShowSettingsButtonProperty, value);
	}

	// Using a DependencyProperty as the backing store for CollapseSearchBox.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty ShowSearchBoxProperty =
		DependencyProperty.Register(nameof(ShowSearchBox), typeof(bool), typeof(AddressToolbar), new(null));
	public bool ShowSearchBox
    {
        get => (bool)GetValue(ShowSearchBoxProperty);
        set => SetValue(ShowSearchBoxProperty, value);
    }

    // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ViewModelProperty =
		DependencyProperty.Register(nameof(ViewModel), typeof(ToolbarViewModel), typeof(AddressToolbar), new PropertyMetadata(null));
	public ToolbarViewModel? ViewModel
	{
		get => (ToolbarViewModel)GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	public StatusCenterViewModel? OngoingTasksViewModel { get; set; }

    public AddressToolbar()
    {
        InitializeComponent();
    }

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        OngoingTasksControl.Initialize(folderViewViewModel);

        FolderViewViewModel = folderViewViewModel;

        UserSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
        Commands ??= folderViewViewModel.GetService<ICommandManager>();
    }

    private void NavToolbar_Loading(FrameworkElement _, object e)
	{
		Loading -= NavToolbar_Loading;
		if (OngoingTasksViewModel is not null)
        {
            OngoingTasksViewModel.NewItemAdded += OngoingTasksActions_ProgressBannerPosted;
        }
    }

	private void VisiblePath_Loaded(object _, RoutedEventArgs e)
	{
		// AutoSuggestBox won't receive focus unless it's fully loaded
		VisiblePath.Focus(FocusState.Programmatic);

		if (DependencyObjectHelpers.FindChild<TextBox>(VisiblePath) is TextBox textBox)
		{
			if (textBox.Text.StartsWith(">"))
            {
                textBox.Select(1, textBox.Text.Length - 1);
            }
            else
            {
                textBox.SelectAll();
            }
        }
	}

	private void ManualPathEntryItem_Click(object _, PointerRoutedEventArgs e)
	{
		if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
		{
			var ptrPt = e.GetCurrentPoint(NavToolbar);
			if (ptrPt.Properties.IsMiddleButtonPressed)
            {
                return;
            }
        }
		ViewModel!.IsEditModeEnabled = true;
	}

	private void VisiblePath_KeyDown(object _, KeyRoutedEventArgs e)
	{
		if (e.Key is VirtualKey.Escape)
        {
            ViewModel!.IsEditModeEnabled = false;
        }
    }
	private void VisiblePath_LostFocus(object _, RoutedEventArgs e)
	{
		var element = FocusManager.GetFocusedElement(FolderViewViewModel.XamlRoot);
		if (element is FlyoutBase or AppBarButton or Popup)
        {
            return;
        }

        if (element is not Control control)
        {
            if (ViewModel!.IsEditModeEnabled)
            {
                ViewModel.IsEditModeEnabled = false;
            }

            return;
        }

        if (control.FocusState is not FocusState.Programmatic and not FocusState.Keyboard)
        {
            ViewModel!.IsEditModeEnabled = false;
        }
        else if (ViewModel!.IsEditModeEnabled)
        {
            VisiblePath.Focus(FocusState.Programmatic);
        }
    }

	private void SearchRegion_OnGotFocus(object sender, RoutedEventArgs e) => ViewModel?.SearchRegion_GotFocus(sender, e);
	private void SearchRegion_LostFocus(object sender, RoutedEventArgs e) => ViewModel?.SearchRegion_LostFocus(sender, e);
#pragma warning disable CA1822 // Mark members as static
    private void SearchRegion_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs _) => sender.Focus(FocusState.Keyboard);
#pragma warning disable CA1822 // Mark members as static

    private void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		=> ViewModel!.VisiblePath_QuerySubmitted(sender, args);

	private void OngoingTasksActions_ProgressBannerPosted(object? _, StatusCenterItem e)
	{
		if (OngoingTasksViewModel is not null)
        {
            OngoingTasksViewModel.NewItemAdded -= OngoingTasksActions_ProgressBannerPosted;
        }

        // Displays a teaching tip the first time a banner is posted
        if (UserSettingsService.AppSettingsService.ShowStatusCenterTeachingTip)
		{
			StatusCenterTeachingTip.IsOpen = true;
			UserSettingsService.AppSettingsService.ShowStatusCenterTeachingTip = false;
		}
	}
}
