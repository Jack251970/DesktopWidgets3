using Microsoft.UI.Dispatching;
using DesktopWidgets3.Helpers;
using Windows.UI.ViewManagement;
using DesktopWidgets3.Models.Widget;
using Windows.Graphics;
using DesktopWidgets3.Views.Pages.Widget;
using DesktopWidgets3.ViewModels.Pages.Widget;
using System.Runtime.InteropServices;
using WinUIEx.Messaging;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class WidgetWindow : WindowEx
{
    #region position & size

    public PointInt32 Position
    {
        get => AppWindow.Position;
        set => WindowExtensions.Move(this, value.X, value.Y);
    }

    public WidgetSize Size
    {
        get => new(AppWindow.Size.Width * 96f / WindowExtensions.GetDpiForWindow(this), AppWindow.Size.Height * 96f / WindowExtensions.GetDpiForWindow(this));
        set => WindowExtensions.SetWindowSize(this, value.Width, value.Height);
    }

    public WidgetSize MinSize
    {
        get => new(MinWidth, MinHeight);
        set
        {
            MinWidth = value.Width;
            MinHeight = value.Height;
        }
    }

    #endregion

    #region type & index

    public WidgetType WidgetType { get; }

    public int IndexTag { get; }

    #endregion

    #region ui elements

    public FrameShellPage? ShellPage => Content as FrameShellPage;

    #endregion

    #region page view model

    public BaseWidgetViewModel? PageViewModel
    {
        get; private set;
    }

    #endregion

    private readonly DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    private readonly WindowManager _manager;

    public WidgetWindow(BaseWidgetItem widgetItem)
    {
        InitializeComponent();

        WidgetType = widgetItem.Type;
        IndexTag = widgetItem.IndexTag;

        Content = null;
        Title = string.Empty;

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        _manager = WindowManager.Get(this);
    }

    // this handles updating the caption button colors correctly when indows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }

    public void InitializeTitleBar()
    {
        IsTitleBarVisible = IsMaximizable = IsMaximizable = false;
    }

    public void InitializeWindow()
    {
        var _handle = this.GetWindowHandle();

        // Hide window icon from taskbar
        SystemHelper.HideWindowIconFromTaskbar(_handle);

        // Get view model of current page
        PageViewModel = (BaseWidgetViewModel?)(ShellPage?.NavigationFrame?.GetPageViewModel());

        // Set window to bottom of other windows
        SystemHelper.SetWindowZPos(_handle, SystemHelper.WINDOWZPOS.ONBOTTOM);

        // Register window sink events
        _manager.WindowMessageReceived += OnWindowMessageReceived;
    }

    private void OnWindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        if (e.Message.MessageId == WM_WINDOWPOSCHANGING)
        {
            var lParam = e.Message.LParam;
            var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
            windowPos.flags |= SWP_NOZORDER;
            Marshal.StructureToPtr(windowPos, lParam, false);

            e.Handled = true;
        }
    }

    #region windows api

    private const int WM_WINDOWPOSCHANGING = 0x0046;
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
