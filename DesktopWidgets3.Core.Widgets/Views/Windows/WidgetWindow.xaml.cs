using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using WinUIEx;
using WinUIEx.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
            if (position != value)
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

    #region Id & Index

    public string Id { get; private set; } = null!;

    public int IndexTag { get; private set; } = -1;

    #endregion

    #region View Model

    public WidgetViewModel ViewModel { get; }

    #endregion

    #region Manager & Handle

    private readonly WindowManager _manager;

    #endregion

    #region Services

    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    #endregion

    public WidgetWindow()
    {
        ViewModel = DependencyExtensions.GetRequiredService<WidgetViewModel>();

        InitializeComponent();

        _manager = WindowManager.Get(this);

        Title = string.Empty;

        // initialize position & size
        position = AppWindow.Position;
        size = new Size(Width, Height);

        // register events
        Activated += WidgetWindow_Activated;
        PositionChanged += WidgetWindow_PositionChanged;
        SizeChanged += WidgetWindow_SizeChanged;
        Closed += WidgetWindow_Closed;
    }

    #region Initialization

    public void InitializeWidgetItem(JsonWidgetItem widgetItem)
    {
        Id = widgetItem.Id;
        IndexTag = widgetItem.IndexTag;
    }

    public void InitializeWindow()
    {
        var hwnd = this.GetWindowHandle();
        SystemHelper.HideWindowFromTaskbar(hwnd); // Hide window icon from taskbar
        SystemHelper.SetWindowZPos(hwnd, SystemHelper.WINDOWZPOS.ONBOTTOM); // Force window to stay at bottom
        _manager.WindowMessageReceived += WindowManager_WindowMessageReceived; // Register window sink events
    }

    #endregion

    #region Events

    private void WidgetWindow_Activated(object? sender, WindowActivatedEventArgs args)
    {
        Activated -= WidgetWindow_Activated;
        var hwnd = this.GetWindowHandle();
        _normalStyle = HwndExtensions.GetWindowStyle(hwnd);  // Initialize normal window style
        HwndExtensions.ToggleWindowStyle(hwnd, false, WindowStyle.TiledWindow);  // Set window style without title bar
        _nonTitleStyle = HwndExtensions.GetWindowStyle(hwnd);  // Initialize edit mode window style
    }

    private void WidgetWindow_PositionChanged(object? sender, PointInt32 e)
    {
        position = e;
    }

    private void WidgetWindow_SizeChanged(object? sender, WindowSizeChangedEventArgs args)
    {
        // update size
        size.Height = Height;
        size.Width = Width;
    }

    private void WidgetWindow_Closed(object? sender, WindowEventArgs args)
    {
        PositionChanged -= WidgetWindow_PositionChanged;
        _manager.WindowMessageReceived -= WindowManager_WindowMessageReceived;
    }

    private void WindowManager_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        switch (e.Message.MessageId)
        {
            case WM_WINDOWPOSCHANGING:
                // force window to stay at bottom
                var lParam = e.Message.LParam;
                var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                windowPos.flags |= SWP_NOZORDER;
                Marshal.StructureToPtr(windowPos, lParam, false);
                e.Handled = true;
                break;
        }
    }

    #endregion

    #region Title Bar

    private WindowStyle _nonTitleStyle;
    private WindowStyle _normalStyle;

    private void SetTitleBarStyle(bool customTitleBar)
    {
        WindowExtensions.SetWindowStyle(this, customTitleBar ? _normalStyle : _nonTitleStyle);
        ExtendsContentIntoTitleBar = customTitleBar;
        SetTitleBar(customTitleBar ? WidgetTitleBar : null);
        IsTitleBarVisible = IsMaximizable = IsMinimizable = false;
    }

    #endregion

    #region Edit Mode

    private bool _isEditMode = true;
    private bool _isEditModeInitialized;

    public async Task SetEditMode(bool isEditMode, MenuFlyout? menuFlyout)
    {
        // check if edit mode is already set
        if (_isEditModeInitialized && _isEditMode == isEditMode)
        {
            return;
        }

        // set window style (it can cause size change)
        IsResizable = isEditMode;

        // set title bar (it can cause size change)
        SetTitleBarStyle(isEditMode);

        // set page update status
        var viewModel = _widgetManagerService.GetWidgetViewModel(this);
        switch (viewModel)
        {
            case IAsyncWidgetUpdate update:
                await update.EnableUpdateAsync(!isEditMode);
                break;
            case IWidgetUpdate update:
                update.EnableUpdate(!isEditMode);
                break;
        }

        // set menu flyout
        ViewModel.WidgetMenuFlyout = isEditMode ? null : menuFlyout;

        // set edit mode flag
        _isEditModeInitialized = true;
        _isEditMode = isEditMode;
    }

    #endregion

    #region Window API

    private const int WM_WINDOWPOSCHANGING = 0x0046;
    private const int WM_DISPLAYCHANGE = 0x007e;
    private const int WM_DPICHANGED = 0x02E0;
    private const int SWP_NOZORDER = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPOS
    {
        internal IntPtr hwnd;
        internal IntPtr hwndInsertAfter;
        internal int x;
        internal int y;
        internal int cx;
        internal int cy;
        internal uint flags;
    }

    #endregion
}
