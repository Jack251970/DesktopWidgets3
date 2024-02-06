// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls;

public enum PreviewPanePositions : ushort
{
	None,
	Right,
	Bottom,
}

public sealed partial class InfoPane : UserControl
{
	public PreviewPanePositions Position { get; private set; } = PreviewPanePositions.None;

	private IInfoPaneSettingsService PaneSettingsService { get; set; } = null!;

    // CHANGE: Use dependency properties instead of fields.
    // Using a DependencyProperty as the backing store for Commands.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CommandsProperty =
        DependencyProperty.Register(nameof(Commands), typeof(ICommandManager), typeof(InfoPane), new PropertyMetadata(null));
    public ICommandManager? Commands
    {
        get => (ICommandManager)GetValue(CommandsProperty);
        set => SetValue(CommandsProperty, value);
    }

    // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(InfoPaneViewModel), typeof(InfoPane), new PropertyMetadata(null));
    public InfoPaneViewModel? ViewModel
    {
        get => (InfoPaneViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private ObservableContext Context { get; } = new();

	public InfoPane()
	{
		InitializeComponent();
		/*PaneSettingsService = DependencyExtensions.GetService<IInfoPaneSettingsService>();
		Commands = DependencyExtensions.GetService<ICommandManager>();
		ViewModel = DependencyExtensions.GetService<InfoPaneViewModel>();*/
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        PaneSettingsService = folderViewViewModel.GetService<IInfoPaneSettingsService>();
        Commands = folderViewViewModel.GetService<ICommandManager>();
        ViewModel = folderViewViewModel.GetService<InfoPaneViewModel>();
    }

	public void UpdatePosition(double panelWidth, double panelHeight)
	{
		if (panelWidth > 700)
		{
			Position = PreviewPanePositions.Right;
			(MinWidth, MinHeight) = (150, 0);
			VisualStateManager.GoToState(this, "Vertical", true);
		}
		else
		{
			Position = PreviewPanePositions.Bottom;
			(MinWidth, MinHeight) = (0, 140);
			VisualStateManager.GoToState(this, "Horizontal", true);
		}
	}

	private void Root_Unloaded(object sender, RoutedEventArgs e)
	{
		PreviewControlPresenter.Content = null;
		Bindings.StopTracking();
	}

	private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
		=> Context.IsHorizontal = Root.ActualWidth >= Root.ActualHeight;

	private void MenuFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e)
		=> ViewModel?.UpdateSelectedItemPreviewAsync(true);

	private void FileTag_PointerEntered(object sender, PointerRoutedEventArgs e)
	{
		VisualStateManager.GoToState((UserControl)sender, "PointerOver", true);
	}

	private void FileTag_PointerExited(object sender, PointerRoutedEventArgs e)
	{
		VisualStateManager.GoToState((UserControl)sender, "Normal", true);
	}

	private class ObservableContext : ObservableObject
	{
		private bool isHorizontal = false;
		public bool IsHorizontal
		{
			get => isHorizontal;
			set => SetProperty(ref isHorizontal, value);
		}
	}
}