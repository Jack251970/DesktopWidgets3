using Windows.Foundation;
using Windows.Graphics;
using WinUIEx;
using WinUIEx.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Windows.Win32;
using Windows.UI.ViewManagement;
using Microsoft.Windows.Widgets;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Automation.Peers;

namespace DesktopWidgets3.Core.Widgets.Views.Windows;

public sealed partial class WidgetWindow : WindowEx
{
    #region Constants

    private static readonly Thickness MicrosoftWidgetScrollViewerPadding = new(8, 8, 8, 8);

    // Adaptive cards render with 8px padding on each side, so we add 8px more of padding on the left and right.
    private static readonly Thickness DesktopWidgets3WidgetScrollViewerPadding = new(16, 8, 16, 8);

    #endregion

    #region Position & Size

    private PointInt32 position;

    /// <summary>
    /// Get or set the position of the window.
    /// </summary>
    /// <remarks>
    /// This property can be used in non-UI thread.
    /// </remarks>
    public PointInt32 Position
    {
        get => position;
        set
        {
            var x = value.X;
            var y = value.Y;
            if (position.X != x || position.Y != y)
            {
                position = value;
                this.Move(value.X, value.Y);
            }
        }
    }

    private Size size;

    /// <summary>
    /// Get or set the size of the window.
    /// </summary>
    /// <remarks>
    /// This property can be used in non-UI thread.
    /// </remarks>
    public RectSize Size
    {
        get => new(size);
        set
        {
            var width = value.Width!.Value;
            var height = value.Height!.Value;
            if (size.Width != width || size.Height != height)
            {
                size = new(width, height);
                this.SetWindowSize(width, height);
            }
        }
    }

    /// <summary>
    /// Diviation size of the window and its content.
    /// This property is not related to Text size of the system (TextScaleFactor) and Scale of the display (DPI).
    /// </summary>
    /// <remarks>
    /// Initialize this property after initializing the styles of the windows, like <see cref="WindowEx.IsResizable"/>.
    /// </remarks>
    private RectSize WindowContentDiviation;

    /// <summary>
    /// Get or set the size of the content in the window.
    /// </summary>
    /// <remarks>
    /// This property is used for recording the <see cref="JsonWidgetItem.Size"/> of the widget.
    /// </remarks>
    public RectSize ContentSize
    {
        get => (Size - WindowContentDiviation) / _uiSettings.TextScaleFactor;
        set
        {
            var textScale = _uiSettings.TextScaleFactor;
            var width = value.Width!.Value * textScale + WindowContentDiviation.Width!.Value;
            var height = value.Height!.Value * textScale + WindowContentDiviation.Height!.Value;
            if (size.Width != width || size.Height != height)
            {
                size = new(width, height);
                this.SetWindowSize(width, height);
            }
        }
    }

    /// <summary>
    /// Get or set the minimum size of the window.
    /// </summary>
    public RectSize MinSize
    {
        get => new(MinWidth, MinHeight);
        set
        {
            if (value.Width != null)
            {
                MinWidth = value.Width.Value;
            }
            if (value.Height != null)
            {
                MinHeight = value.Height.Value;
            }
        }
    }

    /// <summary>
    /// Get or set the maximum size of the window.
    /// </summary>
    public RectSize MaxSize
    {
        get => new(MaxWidth, MaxHeight);
        set
        {
            if (value.Width != null)
            {
                MaxWidth = value.Width.Value;
            }
            if (value.Height != null)
            {
                MaxHeight = value.Height.Value;
            }
        }
    }

    #endregion

    #region Widget Info

    public WidgetProviderType ProviderType { get; private set; }

    public string WidgetId { get; private set; }

    public string WidgetType { get; private set; }

    public int WidgetIndex { get; private set; }

    public string RuntimeId { get; private set; }

    private BaseWidgetSettings? WidgetSettings;

    private WidgetViewModel? WidgetViewModel;

    private PointInt32 _widgetPosition;

    private RectSize _widgetSize;

    #endregion

    #region Active

    private bool isActive = false;
    public bool IsActive
    {
        get => isActive;
        private set
        {
            if (isActive != value)
            {
                isActive = value;
                OnIsActiveChanged();
            }
        }
    }

    #endregion

    #region View Model

    public WidgetWindowViewModel ViewModel { get; }

    #endregion

    #region Manager & UI Settings

    private readonly WindowManager _manager;

    private readonly UISettings _uiSettings = new();

    #endregion

    #region Services

    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();
    private readonly IWidgetResourceService _widgetResourceService = DependencyExtensions.GetRequiredService<IWidgetResourceService>();

    #endregion

    #region Constructor

    public WidgetWindow(string widgetRuntimeId, JsonWidgetItem widgetItem)
    {
        // Initialize widget info
        ProviderType = widgetItem.ProviderType;
        WidgetId = widgetItem.Id;
        WidgetType = widgetItem.Type;
        WidgetIndex = widgetItem.Index;
        RuntimeId = widgetRuntimeId;
        WidgetSettings = widgetItem.Settings;
        WidgetViewModel = null;

        // Initialize view model
        ViewModel = DependencyExtensions.GetRequiredService<WidgetWindowViewModel>();

        // Initialize ui elements
        InitializeComponent();

        // Initialize widget size & position for completed event
        var widgetSizeHeight = widgetItem.Size.Height!.Value;
        var widgetSizeWidth = widgetItem.Size.Width!.Value;
        _widgetSize = new RectSize(widgetSizeWidth, widgetSizeHeight);
        _widgetPosition = AppWindow.Position;
        if (widgetItem.Position.X != WidgetConstants.DefaultWidgetPosition.X)
        {
            _widgetPosition.X = widgetItem.Position.X;
        }
        if (widgetItem.Position.Y != WidgetConstants.DefaultWidgetPosition.Y)
        {
            _widgetPosition.Y = widgetItem.Position.Y;
        }

        // Initialize properties for ui elements
        WidgetScrollViewer.Padding = DesktopWidgets3WidgetScrollViewerPadding;

        // Initialize manager & title for window
        _manager = WindowManager.Get(this);
        Title = string.Empty;

        // Initiliaze size & position for window
        (var minSize, var maxSize) = _widgetResourceService.GetWidgetMinMaxSize(ProviderType, widgetItem.Id, widgetItem.Type);
        MinSize = minSize;
        MaxSize = maxSize;
        Size = _widgetSize;
        position = AppWindow.Position;

        // Register events
        Activated += WidgetWindow_Activated;
        Closed += WidgetWindow_Closed;
        PositionChanged += WidgetWindow_PositionChanged;
        SizeChanged += WidgetWindow_SizeChanged;
    }

    public WidgetWindow(string widgetRuntimeId, JsonWidgetItem widgetItem, WidgetViewModel widgetViewModel)
    {
        // Initialize widget info
        ProviderType = widgetItem.ProviderType;
        WidgetId = widgetItem.Id;
        WidgetType = widgetItem.Type;
        WidgetIndex = widgetItem.Index;
        RuntimeId = widgetRuntimeId;
        WidgetSettings = null;
        WidgetViewModel = widgetViewModel;

        // Initialize view model
        ViewModel = DependencyExtensions.GetRequiredService<WidgetWindowViewModel>();

        // Initialize ui elements
        InitializeComponent();

        // Initialize size & position for completed event
        var widgetSizeHeight = GetPixelHeightFromWidgetSize(WidgetViewModel.WidgetSize);
        var widgetSizeWidth = WidgetHelpers.WidgetPxWidth;
        _widgetSize = new RectSize(widgetSizeWidth, widgetSizeHeight);
        _widgetPosition = AppWindow.Position;
        if (widgetItem.Position.X != WidgetConstants.DefaultWidgetPosition.X)
        {
            _widgetPosition.X = widgetItem.Position.X;
        }
        if (widgetItem.Position.Y != WidgetConstants.DefaultWidgetPosition.Y)
        {
            _widgetPosition.Y = widgetItem.Position.Y;
        }

        // Initialize properties for ui elements
        WidgetScrollViewer.Padding = MicrosoftWidgetScrollViewerPadding;

        // Initialize manager & title for window
        _manager = WindowManager.Get(this);
        Title = string.Empty;

        // Initiliaze size & position for window
        (var minSize, var maxSize) = _widgetResourceService.GetWidgetMinMaxSize(ProviderType, widgetItem.Id, widgetItem.Type);
        MinSize = minSize;
        MaxSize = maxSize;
        Size = _widgetSize;
        position = AppWindow.Position;

        // Register events
        Activated += WidgetWindow_Activated;
        Closed += WidgetWindow_Closed;
        PositionChanged += WidgetWindow_PositionChanged;
        SizeChanged += WidgetWindow_SizeChanged;
    }

    #endregion

    #region Initialization

    public async Task InitializeAsync(WidgetViewModel? widgetViewModel = null)
    {
        // set window properties
        var hwnd = this.GetWindowHandle();
        SystemHelper.SetWindowZPos(hwnd, SystemHelper.WINDOWZPOS.ONBOTTOM); // Force window to stay at bottom
        _manager.WindowMessageReceived += WindowManager_WindowMessageReceived; // Register window sink events

        // initialize widget view model
        await SetWidgetMenuAsync(widgetViewModel);
    }

    #endregion

    #region Menu Flyout

    private MenuFlyout? WidgetMenuFlyout;

    private async Task SetWidgetMenuAsync(WidgetViewModel? widgetViewModel)
    {
        if (WidgetMenuFlyout == null)
        {
            WidgetMenuFlyout = new MenuFlyout
            {
                Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
            };

            // pin & delete & customize
            AddUnpinDeleteItemsToWidgetMenu(WidgetMenuFlyout);
            if (widgetViewModel != null && widgetViewModel.WidgetDefinition.IsCustomizable)
            {
                AddCustomizeToWidgetMenu(WidgetMenuFlyout);
            }

            // size
            if (ProviderType == WidgetProviderType.Microsoft)
            {
                WidgetMenuFlyout.Items.Add(new MenuFlyoutSeparator());
                await AddSizeItemsToWidgetMenuAsync(WidgetMenuFlyout, widgetViewModel);
            }

            // layout
            WidgetMenuFlyout.Items.Add(new MenuFlyoutSeparator());
            AddLayoutItemsToWidgetMenu(WidgetMenuFlyout);

#if DEBUG
            // restart
            WidgetMenuFlyout.Items.Add(new MenuFlyoutSeparator());
            AddRestartItemsToWidgetMenu(WidgetMenuFlyout);
#endif
        }
    }

    #region Unpin & Delete

    private void AddUnpinDeleteItemsToWidgetMenu(MenuFlyout menuFlyout)
    {
        var unpinIcon = new FontIcon()
        {
            Glyph = "\uE77A"
        };
        var unpinWidgetMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_UnpinWidget.Text".GetLocalizedString(),
            Icon = unpinIcon
        };
        unpinWidgetMenuItem.Click += OnUnpinWidgetClick;
        menuFlyout.Items.Add(unpinWidgetMenuItem);

        var deleteIcon = new FontIcon()
        {
            Glyph = "\uE74D"
        };
        var deleteWidgetMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_DeleteWidget.Text".GetLocalizedString(),
            Icon = deleteIcon
        };
        deleteWidgetMenuItem.Click += OnDeleteWidgetClick;
        menuFlyout.Items.Add(deleteWidgetMenuItem);
    }

    private async void OnUnpinWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem)
        {
            await _widgetManagerService.UnpinWidgetAsync(ProviderType, WidgetId, WidgetType, WidgetIndex, true);
        }
    }

    private async void OnDeleteWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem)
        {
            await DialogFactory.ShowDeleteWidgetFullScreenDialogAsync(async () =>
            {
                await _widgetManagerService.DeleteWidgetAsync(ProviderType, WidgetId, WidgetType, WidgetIndex, true);
            });
        }
    }

    #endregion

    #region Customize

    private void AddCustomizeToWidgetMenu(MenuFlyout widgetMenuFlyout)
    {
        var icon = new FontIcon()
        {
            Glyph = "\xE70F"
        };
        var customizeWidgetItem = new MenuFlyoutItem
        {
            Icon = icon,
            Text = "MenuFlyoutItem_CustomizeWidget.Text".GetLocalizedString()
        };
        customizeWidgetItem.Click += OnCustomizeWidgetClick;
        widgetMenuFlyout.Items.Add(customizeWidgetItem);
    }

    private async void OnCustomizeWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem)
        {
            if (ProviderType == WidgetProviderType.DesktopWidgets3)
            {
                // TODO(Future): Add support for desktop widgets customization and combine this codes in one function.
            }
            else
            {
                await ViewModel.WidgetViewModel!.Widget.NotifyCustomizationRequestedAsync();
            }
        }
    }

    #endregion

    #region Size

    private SelectableMenuFlyoutItem? _currentSelectedSize;

    private async Task AddSizeItemsToWidgetMenuAsync(MenuFlyout widgetMenuFlyout, WidgetViewModel? widgetViewModel)
    {
        if (widgetViewModel == null)
        {
            // If we can't get the widgetViewModel, bail and don't show sizes.
            return;
        }

        var widgetHostingService = DependencyExtensions.GetRequiredService<IWidgetHostingService>();
        var unsafeWidgetDefinition = await widgetHostingService.GetWidgetDefinitionAsync(widgetViewModel.Widget.DefinitionId);
        if (unsafeWidgetDefinition == null)
        {
            // If we can't get the widgetDefinition, bail and don't show sizes.
            return;
        }

        var widgetDefinitionId = await ComSafeWidgetDefinition.GetIdFromUnsafeWidgetDefinitionAsync(unsafeWidgetDefinition);
        if (string.IsNullOrEmpty(widgetDefinitionId))
        {
            // If we can't get the widgetDefinitionId, bail and don't show sizes.
            return;
        }

        var comSafeWidgetDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
        if (!await comSafeWidgetDefinition.PopulateAsync())
        {
            // If we can't populate the widgetDefinition, bail and don't show sizes.
            return;
        }

        var capabilities = await comSafeWidgetDefinition.GetWidgetCapabilitiesAsync();
        var sizeMenuItems = new List<SelectableMenuFlyoutItem>();

        // Add the three possible sizes. Each side should only be enabled if it is included in the widget's capabilities.
        if (capabilities.Any(cap => cap.Size == WidgetSize.Small))
        {
            var menuItemSmall = new SelectableMenuFlyoutItem
            {
                Tag = WidgetSize.Small,
                Text = "SmallWidgetMenuText".GetLocalizedString(Constants.DevHomeDashboard)
            };
            menuItemSmall.Click += OnMenuItemSizeClick;
            widgetMenuFlyout.Items.Add(menuItemSmall);
            sizeMenuItems.Add(menuItemSmall);
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Medium))
        {
            var menuItemMedium = new SelectableMenuFlyoutItem
            {
                Tag = WidgetSize.Medium,
                Text = "MediumWidgetMenuText".GetLocalizedString(Constants.DevHomeDashboard)
            };
            menuItemMedium.Click += OnMenuItemSizeClick;
            widgetMenuFlyout.Items.Add(menuItemMedium);
            sizeMenuItems.Add(menuItemMedium);
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Large))
        {
            var menuItemLarge = new SelectableMenuFlyoutItem
            {
                Tag = WidgetSize.Large,
                Text = "LargeWidgetMenuText".GetLocalizedString(Constants.DevHomeDashboard)
            };
            menuItemLarge.Click += OnMenuItemSizeClick;
            widgetMenuFlyout.Items.Add(menuItemLarge);
            sizeMenuItems.Add(menuItemLarge);
        }

        // Mark current widget size.
        var currentSelectedSize = sizeMenuItems.FirstOrDefault(x => (WidgetSize)x.Tag == widgetViewModel.WidgetSize);
        _currentSelectedSize = currentSelectedSize;
        if (currentSelectedSize is not null)
        {
            MarkSize(currentSelectedSize);
        }
    }

    private async void OnMenuItemSizeClick(object sender, RoutedEventArgs e)
    {
        if (sender is SelectableMenuFlyoutItem menuSizeItem && menuSizeItem.Tag is WidgetSize size)
        {
            // Unset mark on current size.
            if (_currentSelectedSize is SelectableMenuFlyoutItem currentSelectedSize)
            {
                currentSelectedSize.Icon = null;
                var peer = FrameworkElementAutomationPeer.FromElement(currentSelectedSize) as SelectableMenuFlyoutItemAutomationPeer;
                peer?.RemoveFromSelection();
            }

            // Resize widget.
            var widgetViewModel = ViewModel.WidgetViewModel!;
            widgetViewModel.WidgetSize = size;
            await widgetViewModel.Widget.SetSizeAsync(size);
            SetScaledWindowSizeAndContentSize(_uiSettings.TextScaleFactor);

            // Set mark on new size.
            _currentSelectedSize = menuSizeItem;
            MarkSize(menuSizeItem);
        }
    }

    private static void MarkSize(SelectableMenuFlyoutItem menuSizeItem)
    {
        var fontIcon = new FontIcon
        {
            Glyph = "\xE915"
        };
        menuSizeItem.Icon = fontIcon;
        var peer = FrameworkElementAutomationPeer.FromElement(menuSizeItem) as SelectableMenuFlyoutItemAutomationPeer;
        peer?.AddToSelection();
    }

    #endregion

    #region Layout

    private void AddLayoutItemsToWidgetMenu(MenuFlyout menuFlyout)
    {
        var layoutIcon = new FontIcon()
        {
            Glyph = "\uF0E2"
        };
        var editLayoutMenuItem = new MenuFlyoutItem
        {
            Icon = layoutIcon,
            Text = "MenuFlyoutItem_EditWidgetsLayout.Text".GetLocalizedString()
        };
        editLayoutMenuItem.Click += OnEditWidgetsLayoutClick;
        menuFlyout.Items.Add(editLayoutMenuItem);
    }

    private async void OnEditWidgetsLayoutClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem)
        {
            await _widgetManagerService.EnterEditModeAsync();
        }
    }

    #endregion

    #region Restart

    private void AddRestartItemsToWidgetMenu(MenuFlyout menuFlyout)
    {
        var restartIcon = new FontIcon()
        {
            Glyph = "\uE72C"
        };
        var restartWidgetMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_RestartWidget.Text".GetLocalizedString(),
            Icon = restartIcon,
        };
        restartWidgetMenuItem.Click += OnRestartWidgetClick;
        menuFlyout.Items.Add(restartWidgetMenuItem);

        restartIcon = new FontIcon()
        {
            Glyph = "\uE72C"
        };
        var restartWidgetsMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_RestartWidgets.Text".GetLocalizedString(),
            Icon = restartIcon
        };
        restartWidgetsMenuItem.Click += OnRestartWidgetsClick;
        menuFlyout.Items.Add(restartWidgetsMenuItem);
    }

    private async void OnRestartWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem)
        {
            await _widgetManagerService.RestartWidgetAsync(ProviderType, WidgetId, WidgetType, WidgetIndex);
        }
    }

    private async void OnRestartWidgetsClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem)
        {
            await _widgetManagerService.RestartAllWidgetsAsync();
        }
    }

    #endregion

    #endregion

    #region Size

    public void SetScaledWindowSizeAndContentSize(double textScale)
    {
        if (ProviderType == WidgetProviderType.DesktopWidgets3)
        {
            ViewModel.HeaderHeight = new GridLength(WidgetHelpers.HeaderHeightUnscaled * textScale);
            ContentArea.Height = double.NaN;  // Use auto height
            ContentArea.Width = double.NaN;  // Use auto width
        }
        else
        {
            var widgetSizeHeight = GetPixelHeightFromWidgetSize(WidgetViewModel!.WidgetSize);
            var widgetSizeWidth = WidgetHelpers.WidgetPxWidth;
            _widgetSize = new RectSize(widgetSizeWidth, widgetSizeHeight);
            ViewModel.HeaderHeight = new GridLength(WidgetHelpers.HeaderHeightUnscaled * textScale);
            ContentArea.Height = widgetSizeHeight * textScale;
            ContentArea.Width = widgetSizeWidth * textScale;
        }

        // initialize diviation size between window and its content
        WindowContentDiviation.Height = Height - Bounds.Height;
        WindowContentDiviation.Width = Width - Bounds.Width;

        // set content size
        ContentSize = _widgetSize;

        // recalculate diviation size (the window pixel size is integer so our content size can be truncated, so
        // we need to fill the gap here so that content size will not changed when user saves the widget without resizing)
        WindowContentDiviation.Height = Size.Height - _widgetSize.Height * textScale;
        WindowContentDiviation.Width = Size.Width - _widgetSize.Width * textScale;
    }

    private static double GetPixelHeightFromWidgetSize(WidgetSize size)
    {
        return size switch
        {
            WidgetSize.Small => WidgetHelpers.WidgetPxHeightSmall,
            WidgetSize.Medium => WidgetHelpers.WidgetPxHeightMedium,
            WidgetSize.Large => WidgetHelpers.WidgetPxHeightLarge,
            _ => 0,
        };
    }

    #endregion

    #region Events

    private void WidgetWindow_Activated(object? sender, WindowActivatedEventArgs args)
    {
        Activated -= WidgetWindow_Activated;
        if (Content is not FrameworkElement content || content.IsLoaded)
        {
            Content_Loaded(this, new RoutedEventArgs());
        }
        else
        {
            content.Loaded += Content_Loaded;
        }
    }

    private async void Content_Loaded(object sender, RoutedEventArgs e)
    {
        // initialize widget icon
        await UpdateWidgetHeaderIconFillAsync(ContentArea.ActualTheme);

        // set title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(ContentArea);

        // set edit mode (it can cause window size change)
        SetEditMode(false);

        // set window size and content size
        SetScaledWindowSizeAndContentSize(_uiSettings.TextScaleFactor);

        // register events
        AppWindow.Changed += AppWindow_Changed;
        _uiSettings.TextScaleFactorChanged += HandleTextScaleFactorChangedAsync;

        // envoke completed event handler
        LoadCompleted?.Invoke(this, new LoadCompletedEventArgs() 
        { 
            WidgetRuntimeId = RuntimeId,
            WidgetPosition = _widgetPosition,
            WidgetSettings = WidgetSettings,
            WidgetViewModel = WidgetViewModel
        });
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs e)
    {
        SetTitleBarDragRegion(_isEditMode);
    }

    private void WidgetWindow_PositionChanged(object? sender, PointInt32 e)
    {
        position = e;
    }

    private void WidgetWindow_SizeChanged(object? sender, WindowSizeChangedEventArgs args)
    {
        size.Height = Height;
        size.Width = Width;
    }

    private void WidgetWindow_Closed(object? sender, WindowEventArgs args)
    {
        WidgetSettings = null;
        WidgetViewModel = null;

        // DevHome does this, but I'm not sure if it's necessary
        Bindings.StopTracking();

        Closed -= WidgetWindow_Closed;
        AppWindow.Changed -= AppWindow_Changed;
        PositionChanged -= WidgetWindow_PositionChanged;
        SizeChanged -= WidgetWindow_SizeChanged;
        _uiSettings.TextScaleFactorChanged -= HandleTextScaleFactorChangedAsync;
        _manager.WindowMessageReceived -= WindowManager_WindowMessageReceived;
    }

    private void WindowManager_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        if (e.Message.MessageId == PInvoke.WM_WINDOWPOSCHANGING)
        {
            SystemHelper.ForceWindowPosition(e.Message.LParam);
            e.Handled = true;
            return;
        }
    }

    private void HandleTextScaleFactorChangedAsync(UISettings sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            SetScaledWindowSizeAndContentSize(sender.TextScaleFactor);
        });
    }

    private async void ContentArea_ActualThemeChanged(FrameworkElement sender, object args)
    {
        await UpdateWidgetHeaderIconFillAsync(sender.ActualTheme);
    }

    private async Task UpdateWidgetHeaderIconFillAsync(ElementTheme actualTheme)
    {
        if (ProviderType == WidgetProviderType.DesktopWidgets3)
        {
            ViewModel.WidgetIconFill = await _widgetResourceService.GetWidgetIconBrushAsync(ProviderType, WidgetId, WidgetType, actualTheme);
        }
        else
        {
            ViewModel.WidgetIconFill = await _widgetResourceService.GetWidgetIconBrushAsync(WidgetViewModel!.WidgetDefinition, actualTheme);
        }
    }

    #endregion

    #region Event Handler

    public event EventHandler<LoadCompletedEventArgs>? LoadCompleted;

    public class LoadCompletedEventArgs : EventArgs
    {
        public required string WidgetRuntimeId { get; set; }

        public required PointInt32 WidgetPosition { get; set; }

        public required BaseWidgetSettings? WidgetSettings { get; set; }

        public required WidgetViewModel? WidgetViewModel { get; set; }
    }

    #endregion

    #region Title Bar

    private void SetTitleBarDragRegion(bool isEditMode)
    {
        this.RaiseSetTitleBarDragRegion(isEditMode ? SetTitleBarDragRegionAllDrag : SetTitleBarDragRegionNoDrag);
    }

    private int SetTitleBarDragRegionAllDrag(InputNonClientPointerSource source, SizeInt32 size, double scaleFactor, Func<UIElement, RectInt32?, RectInt32> getScaledRect)
    {
        return -1;
    }

    private int SetTitleBarDragRegionNoDrag(InputNonClientPointerSource source, SizeInt32 size, double scaleFactor, Func<UIElement, RectInt32?, RectInt32> getScaledRect)
    {
        source.SetRegionRects(NonClientRegionKind.Passthrough, [getScaledRect(ContentArea, null)]);
        return -1;
    }

    #endregion

    #region Edit Mode

    private bool _isEditMode = true;
    private bool _isEditModeInitialized = false;

    public void SetEditMode(bool isEditMode)
    {
        // check if edit mode is already set
        if (_isEditModeInitialized && _isEditMode == isEditMode)
        {
            return;
        }

        // set window style (it can cause size change)
        if (ProviderType == WidgetProviderType.DesktopWidgets3)
        {
            IsResizable = isEditMode;
        }

        // set title bar (it can cause size change)
        SetTitleBarDragRegion(isEditMode);

        // set page update status
        if (!_isEditModeInitialized)
        {
            isActive = !isEditMode;
        }
        else
        {
            IsActive = !isEditMode;
        }

        // set widget menu
        ViewModel.WidgetMenuFlyout = isEditMode ? null : WidgetMenuFlyout;

        // set edit mode flag
        _isEditModeInitialized = true;
        _isEditMode = isEditMode;
    }

    #endregion

    #region Activate & Deactivate

    public void OnIsActiveChanged()
    {
        // get widget info
        var providerType = ProviderType;
        var widgetId = WidgetId;
        var widgetType = WidgetType;
        var widgetIndex = WidgetIndex;

        // get widget context
        var widgetContext = _widgetManagerService.GetWidgetContext(providerType, widgetId, widgetType, widgetIndex);

        // invoke activate or deactivate event
        if (isActive)
        {
            _widgetResourceService.ActivateWidget(widgetId, widgetContext!);
        }
        else
        {
            _widgetResourceService.DeactivateWidget(widgetId, RuntimeId);
        }
    }

    #endregion
}
