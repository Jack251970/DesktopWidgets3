using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using WinUIEx;
using WinUIEx.Messaging;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Views.Windows;

public sealed partial class WidgetWindow : WindowEx
{
    #region Position & Size

    private PointInt32 position;
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

    public int IndexTag { get; private set; }

    #endregion

    #region UI Elements

    public WidgetPage ShellPage => (WidgetPage)Content;

    public FrameworkElement? FrameworkElement => ShellPage.ViewModel.WidgetFrameworkElement;

    #endregion

    #region Manager & Handle

    public WindowManager WindowManager => _manager;
    public IntPtr WindowHandle => _handle;

    private readonly WindowManager _manager;
    private readonly IntPtr _handle;

    #endregion

    #region Services

    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    #endregion

    public WidgetWindow()
    {
        InitializeComponent();

        _manager = WindowManager.Get(this);
        _handle = this.GetWindowHandle();

        Title = string.Empty;
        var content = DependencyExtensions.GetRequiredService<WidgetPage>();
        content.InitializeWindow(this);
        Content = content;

        position = AppWindow.Position;
        PositionChanged += WidgetWindow_PositionChanged;

        size = GetAppWindowSize();
        SizeChanged += WidgetWindow_SizeChanged;
    }

    #region Show & Activate

    private bool activated = false;

    public void Show()
    {
        if (!activated)
        {
            Activate();
        }
        else
        {
            WindowExtensions.Show(this);
        }
    }

    public new void Activate()
    {
        base.Activate();
        activated = true;
    }

    #endregion

    #region Position & Size

    private Size GetAppWindowSize()
    {
        var windowDpi = this.GetDpiForWindow();
        return new(AppWindow.Size.Width * 96f / windowDpi, AppWindow.Size.Height * 96f / windowDpi);
    }

    private void WidgetWindow_PositionChanged(object? sender, PointInt32 e)
    {
        Position = e;
    }

    private void WidgetWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        if (!IsResizable)
        {
            size = args.Size;
        }
        else if (enterEditMode)
        {
            var size = GetAppWindowSize();
            divSize.Width = Math.Ceiling(size.Width - args.Size.Width);
            divSize.Height = Math.Ceiling(size.Height - args.Size.Height);
            enterEditMode = false;
            exitEditMode = true;
        }
    }

    #endregion

    #region Initialization

    public void InitializeWindow(JsonWidgetItem widgetItem)
    {
        Id = widgetItem.Id;
        IndexTag = widgetItem.IndexTag;
        PersistenceId = $"WidgetWindow_{Id}_{IndexTag}";
    }

    public void InitializeTitleBar()
    {
        IsTitleBarVisible = IsMaximizable = IsMinimizable = false;
    }

    public void InitializeWindow()
    {
        // Hide window icon from taskbar
        SystemHelper.HideWindowIconFromTaskbar(_handle);

        // Set window to bottom of other windows
        SystemHelper.SetWindowZPos(_handle, SystemHelper.WINDOWZPOS.ONBOTTOM);

        // Register window sink events
        _manager.WindowMessageReceived += OnWindowMessageReceived;
    }

    #endregion

    #region Edit Mode

    private RectSize divSize = new(0, 0);
    private bool enterEditMode;
    private bool exitEditMode;

    public async Task SetEditMode(bool isEditMode)
    {
        // set flag
        enterEditMode = isEditMode;

        // set window style
        IsResizable = isEditMode;

        // set title bar
        ShellPage.SetCustomTitleBar(isEditMode);

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

        // set window size
        if (isEditMode)
        {
            Size = new RectSize(size.Width + divSize.Width, size.Height + divSize.Height);
        }
        else if (exitEditMode)
        {
            Size = new RectSize(size.Width - divSize.Width, size.Height - divSize.Height);
            exitEditMode = false;
        }
    }

    #endregion

    #region Window Message

    private void OnWindowMessageReceived(object? sender, WindowMessageEventArgs e)
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
            case WM_DISPLAYCHANGE:
                break;
            case WM_DPICHANGED:
                // No need to handle dpi changed event - WinUIEx got this.
                break;
        }
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
