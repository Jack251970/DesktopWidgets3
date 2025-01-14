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

namespace DesktopWidgets3.Core.Widgets.Views.Windows;

// TODO: Improve code quality for this class. Devide it into smaller classes.
public sealed partial class WidgetWindow : WindowEx
{
    #region Constants

    // Adaptive cards render with 8px padding on each side, so we add that to the original padding.
    private static readonly Thickness DesktopWidgets3WidgetScrollViewerPadding = new(16, 8, 16, 8);
    private static readonly Thickness MicrosoftWidgetScrollViewerPadding = new(8, 0, 8, 0);

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
        get => new(size.Width, size.Height);
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

    private double WindowContentDiviationHeight;
    private double WindowContentDiviationWidth;

    /// <summary>
    /// Get or set the size of the content in the window.
    /// </summary>
    /// <remarks>
    /// This property can be used in non-UI thread.
    /// </remarks>
    public RectSize ContentSize
    {
        get => new(size.Width - WindowContentDiviationWidth, size.Height - WindowContentDiviationHeight);
        set
        {
            var width = value.Width!.Value + WindowContentDiviationWidth;
            var height = value.Height!.Value + WindowContentDiviationHeight;
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

    public string RuntimeId { get; private set; }

    private BaseWidgetSettings? WidgetSettings { get; set; }

    private WidgetViewModel? WidgetSource { get; set; }

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
        RuntimeId = widgetRuntimeId;
        WidgetSettings = widgetItem.Settings;
        WidgetSource = null;

        // Initialize view model
        ViewModel = DependencyExtensions.GetRequiredService<WidgetWindowViewModel>();

        // Initialize ui elements
        InitializeComponent();

        // Initialize properties for ui elements
        WidgetScrollViewer.Padding = DesktopWidgets3WidgetScrollViewerPadding;
        var textScale = _uiSettings.TextScaleFactor;
        ViewModel.HeaderHeight = new GridLength(WidgetHelpers.HeaderHeightUnscaled * textScale);

        // Initialize widget size & position for completed event
        _widgetSize = widgetItem.Size;
        _widgetPosition = AppWindow.Position;
        if (widgetItem.Position.X != -10000)
        {
            _widgetPosition.X = widgetItem.Position.X;
        }
        if (widgetItem.Position.Y != -10000)
        {
            _widgetPosition.Y = widgetItem.Position.Y;
        }

        // Initialize manager & title for window
        _manager = WindowManager.Get(this);
        Title = string.Empty;

        // Initiliaze size & position for window
        (var minSize, var maxSize) = _widgetResourceService.GetWidgetMinMaxSize(widgetItem.Id, widgetItem.Type);
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

    public WidgetWindow(WidgetViewModel widgetViewModel)
    {
        // Initialize widget info
        RuntimeId = null!;
        WidgetSettings = null;
        WidgetSource = widgetViewModel;

        // Initialize view model
        ViewModel = DependencyExtensions.GetRequiredService<WidgetWindowViewModel>();

        // Initialize ui elements
        InitializeComponent();

        // Initialize properties for ui elements
        WidgetScrollViewer.Padding = MicrosoftWidgetScrollViewerPadding;
        var textScale = _uiSettings.TextScaleFactor;
        ViewModel.HeaderHeight = new GridLength(WidgetHelpers.HeaderHeightUnscaled * textScale);
        var contentHeight = GetPixelHeightFromWidgetSize(WidgetSource.WidgetSize) * textScale;
        var contentWidth = WidgetHelpers.WidgetPxWidth * textScale;
        ContentArea.Height = contentHeight;
        ContentArea.Width = contentWidth;

        // Initialize size & position for completed event
        _widgetSize = new RectSize(contentWidth, contentHeight);
        _widgetPosition = AppWindow.Position;
        // TODO: Set position.
        /*if (widgetItem.Position.X != -10000)
        {
            _widgetPosition.X = widgetItem.Position.X;
        }
        if (widgetItem.Position.Y != -10000)
        {
            _widgetPosition.Y = widgetItem.Position.Y;
        }*/
        _widgetPosition.X = 20;
        _widgetPosition.Y = 20;

        // Initialize manager & title for window
        _manager = WindowManager.Get(this);
        Title = string.Empty;

        // Initiliaze size & position for window
        MinSize = RectSize.NULL;
        MaxSize = RectSize.NULL;
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

    private MenuFlyout WidgetMenuFlyout { get; set; } = null!;

    public void Initialize(MenuFlyout menuFlyout)
    {
        // set window properties
        var hwnd = this.GetWindowHandle();
        SystemHelper.SetWindowZPos(hwnd, SystemHelper.WINDOWZPOS.ONBOTTOM); // Force window to stay at bottom
        _manager.WindowMessageReceived += WindowManager_WindowMessageReceived; // Register window sink events

        // set widget menu flyout
        WidgetMenuFlyout = menuFlyout;
        ViewModel.WidgetMenuFlyout = menuFlyout;
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

    private void Content_Loaded(object sender, RoutedEventArgs e)
    {
        // set title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(ContentArea);

        // set edit mode (it can cause size change)
        SetEditMode(false);

        // initialize diviation size between window and its content
        WindowContentDiviationHeight = Height - Bounds.Height;
        WindowContentDiviationWidth = Width - Bounds.Width;

        // set content size
        ContentSize = _widgetSize;

        // register events
        AppWindow.Changed += AppWindow_Changed;

        // envoke completed event handler
        LoadCompleted?.Invoke(this, new LoadCompletedEventArgs() 
        { 
            WidgetRuntimeId = RuntimeId,
            WidgetPosition = _widgetPosition,
            WidgetSettings = WidgetSettings,
            WidgetViewModel = WidgetSource
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
        WidgetMenuFlyout = null!;

        Closed -= WidgetWindow_Closed;
        AppWindow.Changed -= AppWindow_Changed;
        PositionChanged -= WidgetWindow_PositionChanged;
        SizeChanged -= WidgetWindow_SizeChanged;
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
    private bool _isEditModeInitialized;

    public void SetEditMode(bool isEditMode)
    {
        // check if edit mode is already set
        if (_isEditModeInitialized && _isEditMode == isEditMode)
        {
            return;
        }

        // set window style (it can cause size change)
        IsResizable = isEditMode;

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

        // set menu flyout
        ViewModel.WidgetMenuFlyout = isEditMode ? null : WidgetMenuFlyout;

        // set edit mode flag
        _isEditModeInitialized = true;
        _isEditMode = isEditMode;
    }

    #endregion

    #region Activate & Deactivate

    public void OnIsActiveChanged()
    {
        // get widget info & context
        var (widgetId, widgetType, widgetIndex) = _widgetManagerService.GetWidgetInfo(RuntimeId);
        var widgetContext = _widgetManagerService.GetWidgetContext(widgetId, widgetType, widgetIndex);

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
}
