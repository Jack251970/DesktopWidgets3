using Windows.Foundation;
using Windows.Graphics;
using WinUIEx;
using WinUIEx.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Windows.Win32;

namespace DesktopWidgets3.Core.Widgets.Views.Windows;

public sealed partial class WidgetWindow : WindowEx
{
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

    private BaseWidgetSettings WidgetSettings { get; set; }

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

    #region Manager & Handle

    private readonly WindowManager _manager;

    #endregion

    #region Services

    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();
    private readonly IWidgetResourceService _widgetResourceService = DependencyExtensions.GetRequiredService<IWidgetResourceService>();

    #endregion

    #region Constructor

    public WidgetWindow(string widgetRuntimeId, JsonWidgetItem widgetItem)
    {
        RuntimeId = widgetRuntimeId;
        WidgetSettings = widgetItem.Settings;
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

        ViewModel = DependencyExtensions.GetRequiredService<WidgetWindowViewModel>();

        InitializeComponent();

        _manager = WindowManager.Get(this);

        Title = string.Empty;

        // initialize position & size
        position = AppWindow.Position;
        size = new Size(Width, Height);

        // register events
        Activated += WidgetWindow_Activated;
        Closed += WidgetWindow_Closed;
        PositionChanged += WidgetWindow_PositionChanged;
        SizeChanged += WidgetWindow_SizeChanged;
    }

    #endregion

    #region Initialization

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

        // set edit mode
        SetEditMode(false);

        // reset the size because the size will change in SetEditMode
        Size = _widgetSize;

        // register events
        AppWindow.Changed += AppWindow_Changed;

        // envoke completed event handler
        LoadCompleted?.Invoke(this, new LoadCompletedEventArgs() 
        { 
            WidgetRuntimeId = RuntimeId,
            WidgetPosition = _widgetPosition,
            WidgetSettings = WidgetSettings
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

        public required BaseWidgetSettings WidgetSettings { get; set; }
    }

    #endregion
}
