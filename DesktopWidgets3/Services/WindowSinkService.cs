using System.Runtime.InteropServices;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using Microsoft.UI.Xaml;
using WinUIEx.Messaging;

namespace DesktopWidgets3.Services;

// <summary>
/// Utils to show window on desktop (at bottom of all windows).
/// https://stackoverflow.com/questions/365094/window-on-desktop
/// </summary>
public class WindowSinkService : IWindowSinkService
{
    #region Windows API

    private const int WM_WINDOWPOSCHANGING = 0x0046;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPOS
    {
        public nint hwnd;
        public nint hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }

    private static readonly nint HWND_BOTTOM = new(1);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);

    #endregion

    #region WindowSinker

    private Window? window;

    private WindowMessageMonitor? monitor;

    private bool _isInitialized;

    public WindowSinkService()
    {

    }

    public void Initialize(Window window)
    {
        if (!_isInitialized)
        {
            this.window = window;

            /*if (page.IsLoaded)
            {
                OnPageLoaded(page, null);
            }
            else
            {
                page.Loaded += OnPageLoaded;
            }
            window!.Activated += OnWindowActivated;*/

            monitor = new WindowMessageMonitor(window);
            monitor.WindowMessageReceived += OnWindowMessageReceived;

            var hWnd = window.GetWindowHandle();
            SystemHelper.HideWindowFromTaskbar(hWnd);

            _isInitialized = true;
        }
    }

    ~WindowSinkService()
    {
        if (_isInitialized)
        {
            // window!.Activated -= OnWindowActivated;
            // page.Loaded -= OnPageLoaded;

            monitor!.Dispose();
        }
    }

    #endregion

    #region Event Handlers

    private void BringToBottom()
    {
        if (window != null)
        {
            // Way1
            var hWnd = window.GetWindowHandle();
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

            // Way2
            var lParam = new nint(Marshal.AllocHGlobal(Marshal.SizeOf<WINDOWPOS>()));
            var windowPos = new WINDOWPOS
            {
                hwnd = hWnd,
                hwndInsertAfter = HWND_BOTTOM,
                x = 0,
                y = 0,
                cx = 0,
                cy = 0,
                flags = SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE
            };
            Marshal.StructureToPtr(windowPos, lParam, false);
        }
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        BringToBottom();
    }

    private void OnPageLoaded(object sender, RoutedEventArgs? e)
    {
        BringToBottom();
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
            e.Result = nint.Zero;
        }
    }

    #endregion
}
