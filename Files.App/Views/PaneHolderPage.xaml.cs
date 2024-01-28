// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;

namespace Files.App.Views;

public sealed partial class PaneHolderPage : Page, IPaneHolder, ITabBarItemContent
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    public static readonly int DualPaneWidthThreshold = 750;

    public event EventHandler<PaneHolderPage>? CurrentInstanceChanged;

    private IUserSettingsService UserSettingsService { get; set; } = null!;

	public bool IsLeftPaneActive
		=> (Page)ActivePane == PaneLeft;

	public bool IsRightPaneActive
		=> (Page)ActivePane == PaneRight;

	public event EventHandler<CustomTabViewItemParameter>? ContentChanged;

	public event PropertyChangedEventHandler? PropertyChanged;

	public IFilesystemHelpers FilesystemHelpers
		=> ActivePane?.FilesystemHelpers!;

	private CustomTabViewItemParameter tabItemArguments = null!;
	public CustomTabViewItemParameter TabItemParameter
	{
		get => tabItemArguments;
		set
		{
			if (tabItemArguments != value)
			{
				tabItemArguments = value;

				ContentChanged?.Invoke(this, value);
			}
		}
	}

	private bool _WindowIsCompact;
	public bool WindowIsCompact
	{
		get => _WindowIsCompact;
		set
		{
			if (value != _WindowIsCompact)
			{
				_WindowIsCompact = value;

				if (value)
				{
					wasRightPaneVisible = IsRightPaneVisible;
					IsRightPaneVisible = false;
				}
				else if (wasRightPaneVisible)
				{
					IsRightPaneVisible = true;
					wasRightPaneVisible = false;
				}

				NotifyPropertyChanged(nameof(IsMultiPaneEnabled));
			}
		}
	}

	private bool wasRightPaneVisible;

	public bool IsMultiPaneActive
		=> IsRightPaneVisible;

	public bool IsMultiPaneEnabled
	{
		get
		{
			if (DependencyExtensions.GetService<AppModel>().IsMainWindowClosed)
            {
                return false;
            }
            else
            {
                return FolderViewViewModel.MainWindow.Bounds.Width > DualPaneWidthThreshold;
            }
        }
	}

	private NavigationParams _NavParamsLeft = null!;
	public NavigationParams NavParamsLeft
	{
		get => _NavParamsLeft;
		set
		{
			if (_NavParamsLeft != value)
			{
				_NavParamsLeft = value;

				NotifyPropertyChanged(nameof(NavParamsLeft));
			}
		}
	}

	private NavigationParams _NavParamsRight = null!;
	public NavigationParams NavParamsRight
	{
		get => _NavParamsRight;
		set
		{
			if (_NavParamsRight != value)
			{
				_NavParamsRight = value;

				NotifyPropertyChanged(nameof(NavParamsRight));
			}
		}
	}

	private IShellPage activePane = null!;
	public IShellPage ActivePane
	{
		get => activePane;
		set
		{
			if (activePane != value)
			{
				activePane = value;

				PaneLeft.IsCurrentInstance = false;

				if (PaneRight is not null)
                {
                    PaneRight.IsCurrentInstance = false;
                }

                if (ActivePane is not null)
                {
                    ActivePane.IsCurrentInstance = IsCurrentInstance;
                }

                NotifyPropertyChanged(nameof(ActivePane));
				NotifyPropertyChanged(nameof(IsLeftPaneActive));
				NotifyPropertyChanged(nameof(IsRightPaneActive));
				NotifyPropertyChanged(nameof(ActivePaneOrColumn));
				NotifyPropertyChanged(nameof(FilesystemHelpers));
			}
		}
	}

	public IShellPage ActivePaneOrColumn
	{
		get
		{
			if (ActivePane is not null && ActivePane.IsColumnView)
            {
                return ((ColumnsLayoutPage)ActivePane.SlimContentPage).ActiveColumnShellPage;
            }

            return ActivePane ?? PaneLeft;
		}
	}

	private bool _IsRightPaneVisible;
	public bool IsRightPaneVisible
	{
		get => _IsRightPaneVisible;
		set
		{
			if (value != _IsRightPaneVisible)
			{
				_IsRightPaneVisible = value;
				if (!_IsRightPaneVisible)
                {
                    ActivePane = PaneLeft;
                }

                Pane_ContentChanged(null!, null!);
				NotifyPropertyChanged(nameof(IsRightPaneVisible));
				NotifyPropertyChanged(nameof(IsMultiPaneActive));
			}
		}
	}

	private bool _IsCurrentInstance;
	public bool IsCurrentInstance
	{
		get => _IsCurrentInstance;
		set
		{
			if (_IsCurrentInstance == value)
            {
                return;
            }

            _IsCurrentInstance = value;
			PaneLeft.IsCurrentInstance = false;

			if (PaneRight is not null)
            {
                PaneRight.IsCurrentInstance = false;
            }

            if (ActivePane is not null)
			{
				ActivePane.IsCurrentInstance = value;

				if (value && ActivePane is BaseShellPage baseShellPage)
                {
                    baseShellPage.ContentPage?.ItemManipulationModel.FocusFileList();
                }
            }

			CurrentInstanceChanged?.Invoke(null, this);
		}
	}

	public PaneHolderPage()
	{
		InitializeComponent();

        /*UserSettingsService = DependencyExtensions.GetService<IUserSettingsService>();*/

        /*MainWindow.Instance.SizeChanged += Current_SizeChanged;
		ActivePane = PaneLeft;
		IsRightPaneVisible = IsMultiPaneEnabled && UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab;*/

        // TODO?: Fallback or an error can occur when failing to get NavigationViewCompactPaneLength value
    }

    private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
		WindowIsCompact = FolderViewViewModel.MainWindow.Bounds.Width <= DualPaneWidthThreshold;
	}

	protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
	{
		base.OnNavigatedTo(eventArgs);

        // CHANGE: Initialize folder view view model and related services.
        if (FolderViewViewModel is null)
        {
            if (eventArgs.Parameter is PanePathNavigationArguments args)
            {
                FolderViewViewModel = args.FolderViewViewModel;
            }
            else if (eventArgs.Parameter is PaneNavigationArguments paneArgs)
            {
                FolderViewViewModel = paneArgs.FolderViewViewModel;
            }

            UserSettingsService = FolderViewViewModel!.GetService<IUserSettingsService>();

            // CHANGE: Initialize context services.
            var pageContext = FolderViewViewModel.GetService<IPageContext>();
            pageContext.Initialize(this);
            FolderViewViewModel.GetService<IContentPageContext>().Initialize(pageContext);

            FolderViewViewModel.MainWindow.SizeChanged += Current_SizeChanged;
            ActivePane = PaneLeft;
            IsRightPaneVisible = IsMultiPaneEnabled && UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab;

            _WindowIsCompact = FolderViewViewModel.MainWindow.Bounds.Width <= DualPaneWidthThreshold;
        }

        if (eventArgs.Parameter is PanePathNavigationArguments panePathArgs)
		{
            NavParamsLeft = new() {
                FolderViewViewModel = FolderViewViewModel!,
                NavPath = panePathArgs.NavPathParam
            };
			NavParamsRight = new() {
                FolderViewViewModel = FolderViewViewModel!,
                NavPath = "Home"
            };
		}
		else if (eventArgs.Parameter is PaneNavigationArguments paneArgs)
		{
            NavParamsLeft = new()
			{
                FolderViewViewModel = FolderViewViewModel!,
				NavPath = paneArgs.LeftPaneNavPathParam,
				SelectItem = paneArgs.LeftPaneSelectItemParam
			};
			NavParamsRight = new()
			{
                FolderViewViewModel = FolderViewViewModel!,
				NavPath = paneArgs.RightPaneNavPathParam,
				SelectItem = paneArgs.RightPaneSelectItemParam
			};

			IsRightPaneVisible = IsMultiPaneEnabled && paneArgs.RightPaneNavPathParam is not null;
		}

        TabItemParameter = new()
		{
            FolderViewViewModel = FolderViewViewModel!,
			InitialPageType = typeof(PaneHolderPage),
			NavigationParameter = new PaneNavigationArguments()
			{
                FolderViewViewModel = FolderViewViewModel!,
				LeftPaneNavPathParam = NavParamsLeft?.NavPath,
				LeftPaneSelectItemParam = NavParamsLeft?.SelectItem,
				RightPaneNavPathParam = IsRightPaneVisible ? NavParamsRight?.NavPath : null,
				RightPaneSelectItemParam = IsRightPaneVisible ? NavParamsRight?.SelectItem : null,
			}
		};
	}

	private void PaneResizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
	{
		if (PaneRight is not null && PaneRight.ActualWidth <= 300)
        {
            IsRightPaneVisible = false;
        }

        this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
	}

	private void Pane_ContentChanged(object sender, CustomTabViewItemParameter e)
	{
		TabItemParameter = new()
		{
            FolderViewViewModel = FolderViewViewModel!,
			InitialPageType = typeof(PaneHolderPage),
			NavigationParameter = new PaneNavigationArguments()
			{
                FolderViewViewModel = FolderViewViewModel!,
				LeftPaneNavPathParam = PaneLeft.TabItemParameter?.NavigationParameter as string ?? e?.NavigationParameter as string,
				RightPaneNavPathParam = IsRightPaneVisible ? PaneRight?.TabItemParameter?.NavigationParameter as string : null
			}
		};
	}

	public Task TabItemDragOver(object sender, DragEventArgs e)
		=> ActivePane?.TabItemDragOver(sender, e) ?? Task.CompletedTask;

	public Task TabItemDrop(object sender, DragEventArgs e)
		=> ActivePane?.TabItemDrop(sender, e) ?? Task.CompletedTask;

	public void OpenPathInNewPane(string path)
	{
		IsRightPaneVisible = true;
		NavParamsRight = new() {
            FolderViewViewModel = FolderViewViewModel!,
            NavPath = path
        };
		ActivePane = PaneRight;
	}

	/*private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		args.Handled = true;
		var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
		var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
		var menu = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);

		switch (c: ctrl, s: shift, m: menu, k: args.KeyboardAccelerator.Key)
		{
			case (true, true, false, VirtualKey.Left): // ctrl + shift + "<-" select left pane
				ActivePane = PaneLeft;
				break;

			case (true, true, false, VirtualKey.Right): // ctrl + shift + "->" select right pane
				if (string.IsNullOrEmpty(NavParamsRight?.NavPath))
				{
					NavParamsRight = new NavigationParams { NavPath = "Home" };
				}
				IsRightPaneVisible = true;
				ActivePane = PaneRight;
				break;
		}
	}*/

	private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void CloseActivePane()
	{
		// NOTE: Can only close right pane at the moment
		IsRightPaneVisible = false;
		PaneLeft.Focus(FocusState.Programmatic);
	}

	private void Pane_Loaded(object sender, RoutedEventArgs e)
	{
		((UIElement)sender).GotFocus += Pane_GotFocus;
		((UIElement)sender).RightTapped += Pane_RightTapped;
	}

	private void Pane_GotFocus(object sender, RoutedEventArgs e)
	{
		var isLeftPane = (Page)sender == PaneLeft;
		if (isLeftPane && (PaneRight?.SlimContentPage?.IsItemSelected ?? false))
		{
			PaneRight.SlimContentPage.LockPreviewPaneContent = true;
			PaneRight.SlimContentPage.ItemManipulationModel.ClearSelection();
			PaneRight.SlimContentPage.LockPreviewPaneContent = false;
		}
		else if (!isLeftPane && (PaneLeft?.SlimContentPage?.IsItemSelected ?? false))
		{
			PaneLeft.SlimContentPage.LockPreviewPaneContent = true;
			PaneLeft.SlimContentPage.ItemManipulationModel.ClearSelection();
			PaneLeft.SlimContentPage.LockPreviewPaneContent = false;
		}

		var activePane = isLeftPane ? PaneLeft : PaneRight;
		if ((Page)ActivePane != activePane)
        {
            ActivePane = activePane!;
        }
    }

	private void Pane_RightTapped(object sender, RoutedEventArgs e)
	{
		if (sender != ActivePane && sender is IShellPage shellPage && shellPage.SlimContentPage is not ColumnsLayoutPage)
        {
            ((UIElement)sender).Focus(FocusState.Programmatic);
        }
    }

	public void Dispose()
	{
		FolderViewViewModel.MainWindow.SizeChanged -= Current_SizeChanged;
		PaneLeft?.Dispose();
		PaneRight?.Dispose();
		PaneResizer.DoubleTapped -= PaneResizer_OnDoubleTapped;
	}

	private void PaneResizer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
	{
		LeftColumn.Width = new GridLength(1, GridUnitType.Star);
		RightColumn.Width = new GridLength(1, GridUnitType.Star);
	}

	private void PaneResizer_Loaded(object sender, RoutedEventArgs e)
	{
		PaneResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
	}

	private void PaneResizer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
	{
		this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
	}
}
