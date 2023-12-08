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

    private const uint SWP_NOZORDER = 0x0004;

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

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);

    #endregion

    #region WindowSinker

    private WindowMessageMonitor? monitor;

    private bool _isInitialized;

    public WindowSinkService() { }
    
    public void Initialize(Window window, bool hideFromTaskBar)
    {
        if (!_isInitialized)
        {
            monitor = new WindowMessageMonitor(window);
            monitor.WindowMessageReceived += OnWindowMessageReceived;

            if (hideFromTaskBar)
            {
                var hWnd = window.GetWindowHandle();
                SystemHelper.HideWindowFromTaskbar(hWnd);
            }

            _isInitialized = true;
        }
    }

    ~WindowSinkService()
    {
        if (_isInitialized)
        {
            monitor!.Dispose();
        }
    }

    #endregion

    #region Event Handlers

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
