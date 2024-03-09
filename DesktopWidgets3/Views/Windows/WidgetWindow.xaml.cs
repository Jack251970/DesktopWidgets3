using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Dispatching;

using System.Runtime.InteropServices;

using Windows.UI.ViewManagement;
using Windows.Graphics;

using WinUIEx.Messaging;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class WidgetWindow : WindowEx
{
    #region position & size

    private PointInt32 position;
    public PointInt32 Position
    {
        get => position;
        set
        {
            if (position != value)
            {
                position = value;
                WindowExtensions.Move(this, value.X, value.Y);
            }
        }
    }

    private Size size;
    public WidgetSize Size
    {
        get => new(size.Width, size.Height);
        set
        {
            var width = value.Width;
            var height = value.Height;
            if (size.Width != width || size.Height != height)
            {
                size = new(width, height);
                WindowExtensions.SetWindowSize(this, width, height);
            }
        }
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

    public WidgetType WidgetType { get; private set; }

    public int IndexTag { get; private set; }

    #endregion

    #region ui elements

    public FrameShellPage ShellPage => (FrameShellPage)Content;

    #endregion

    #region page view model & settings

    public ObservableRecipient? PageViewModel { get; private set; }

    public BaseWidgetSettings Settings => ((IWidgetSettings)PageViewModel!).GetWidgetSettings();

    #endregion

    #region manager & handle

    public WindowManager WindowManager => _manager;
    public IntPtr WindowHandle => _handle;

    private readonly WindowManager _manager;
    private readonly IntPtr _handle;

    #endregion

    private readonly UISettings settings;

    public WidgetWindow()
    {
        InitializeComponent();

        _manager = WindowManager.Get(this);
        _handle = this.GetWindowHandle();

        Content = null;
        Title = string.Empty;

        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        position = AppWindow.Position;
        PositionChanged += WidgetWindow_PositionChanged;

        size = GetAppWindowSize();
        SizeChanged += WidgetWindow_SizeChanged;
    }

    // This handles updating the caption button colors correctly when indows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.High, () => TitleBarHelper.ApplySystemThemeToCaptionButtons(this, null));
    }

    #region position & size

    private Size GetAppWindowSize()
    {
        var windowDpi = WindowExtensions.GetDpiForWindow(this);
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

    #region edit mode

    private WidgetSize divSize = new(0,0);
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
        if (PageViewModel is IWidgetUpdate viewModel)
        {
            await viewModel.EnableUpdate(!isEditMode);
        }

        // change window size
        if (isEditMode)
        {
            Size = new WidgetSize(size.Width + divSize.Width, size.Height + divSize.Height);
        }
        else if (exitEditMode)
        {
            Size = new WidgetSize(size.Width - divSize.Width, size.Height - divSize.Height);
            exitEditMode = false;
        }
    }

    #endregion

    #region initialize

    public void InitializeSettings(BaseWidgetItem widgetItem)
    {
        WidgetType = widgetItem.Type;
        IndexTag = widgetItem.IndexTag;
        ShellPage.ViewModel.WidgetNavigationService.NavigateTo(WidgetType, new WidgetNavigationParameter()
        {
            Window = this,
            Settings = widgetItem.Settings
        });
    }

    public void InitializeTitleBar()
    {
        IsTitleBarVisible = IsMaximizable = IsMaximizable = false;
    }

    public void InitializeWindow()
    {
        // Hide window icon from taskbar
        SystemHelper.HideWindowIconFromTaskbar(_handle);

        // Get view model of current page
        PageViewModel = ShellPage.NavigationFrame.GetPageViewModel() as ObservableRecipient;

        // Set window to bottom of other windows
        SystemHelper.SetWindowZPos(_handle, SystemHelper.WINDOWZPOS.ONBOTTOM);

        // Register window sink events
        _manager.WindowMessageReceived += OnWindowMessageReceived;
    }

    #endregion

    #region bottom window

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
