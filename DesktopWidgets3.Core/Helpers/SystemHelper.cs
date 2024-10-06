using System.Runtime.InteropServices;

namespace DesktopWidgets3.Core.Helpers;

/// <summary>
/// Helper for actions related to windows system.
/// </summary>
public partial class SystemHelper
{
    #region check window existence

    internal const int SW_HIDE = 0;
    internal const int SW_SHOW = 5;
    internal const int SW_RESTORE = 9;
    internal const int WM_SHOWWINDOW = 0x0018;
    internal const int SW_PARENTOPENING = 3;

    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    internal static partial IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

    /// <summary>
    /// Check if window exists and show window.
    /// </summary>
    public static bool IsWindowExist(string? className, string? windowName, bool showWindow)
    {
        var handle = FindWindow(className, windowName);
        if (handle != IntPtr.Zero)
        {
            if (showWindow)
            {
                // show window
                ShowWindow(handle, SW_RESTORE);
                ShowWindow(handle, SW_SHOW);
                SendMessage(handle, WM_SHOWWINDOW, 0, SW_PARENTOPENING);
                // bring window to front
                SetForegroundWindow(handle);
            }
            return true;
        }
        return false;
    }

    #endregion

    #region system dialog

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    /// <summary>
    /// Show dialog.
    /// </summary>
    public static int MessageBox(string lpText, string lpCaption, uint uType = 0)
    {
        return MessageBox(new IntPtr(0), lpText, lpCaption, uType);
    }

    #endregion

    #region exit windows

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TokPriv1Luid
    {
        public int Count;
        public long Luid;
        public int Attr;
    }

    [LibraryImport("kernel32.dll")]
    internal static partial IntPtr GetCurrentProcess();

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

    /*[LibraryImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool LookupPrivilegeValue(byte[]? lpSystemName, byte[] name, ref long lpLuid);*/

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool LookupPrivilegeValue(string? host, string name, ref long pluid);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AdjustTokenPrivileges(
        IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
        ref TokPriv1Luid NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ExitWindowsEx(int uFlags, int dwReason);

    internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
    internal const int TOKEN_QUERY = 0x00000008;
    internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
    internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
    internal const int EWX_LOGOFF = 0x00000000;
    internal const int EWX_SHUTDOWN = 0x00000001;
    internal const int EWX_REBOOT = 0x00000002;
    internal const int EWX_FORCE = 0x00000004;
    internal const int EWX_POWEROFF = 0x00000008;
    internal const int EWX_FORCEIFHUNG = 0x00000010;

    internal static void DoExitWin(int flag)
    {
        TokPriv1Luid tp;
        var hproc = GetCurrentProcess();
        var htok = IntPtr.Zero;
        _ = OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok);
        tp.Count = 1;
        tp.Luid = 0;
        tp.Attr = SE_PRIVILEGE_ENABLED;
        _ = LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tp.Luid);
        _ = AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        _ = ExitWindowsEx(flag, 0);
    }

    /// <summary>
    /// Reboot windows system.
    /// </summary>
    public static void SystemReboot()
    {
        DoExitWin(EWX_FORCE | EWX_REBOOT);
    }

    /// <summary>
    /// Shut down windows system.
    /// </summary>
    public static void SystemPowerOff()
    {
        DoExitWin(EWX_FORCE | EWX_POWEROFF);
    }

    /// <summary>
    /// Log off windows system.
    /// </summary>
    public static void SystemLogOff()
    {
        DoExitWin(EWX_FORCE | EWX_LOGOFF);
    }

    #endregion

    #region hide window icon

    internal const int GWL_EXSTYLE = -20;
    internal const int WS_EX_TOOLWINDOW = 0x00000080;

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    internal static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    internal static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    /// <summary>
    /// Hide window icon from taskbar.
    /// </summary>
    public static void HideWindowFromTaskbar(IntPtr hWnd)
    {
        SetWindowLongPtr(hWnd, GWL_EXSTYLE, GetWindowLongPtr(hWnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
        ShowWindow(hWnd, SW_HIDE);
    }

    #endregion

    #region window z position

    public enum WINDOWZPOS
    {
        ONTOP,
        ONBOTTOM,
        ONDESKTOP
    }

    internal const uint GA_PARENT = 1;

    internal const uint WS_EX_TOPMOST = 0x00000008;

    internal const IntPtr HWND_TOPMOST = -1;
    internal const IntPtr HWND_BOTTOM = 1;
    internal const IntPtr HWND_NOTOPMOST = -2;

    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOZORDER = 0x0004;
    internal const uint SWP_NOACTIVATE = 0x0010;

    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetDesktopWindow();

    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    [LibraryImport("user32.dll")]
    internal static partial int GetWindowLong(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll")]
    internal static partial IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    /// <summary>
    /// Set window Z position.
    /// </summary>
    public static void SetWindowZPos(IntPtr hWnd, WINDOWZPOS pos)
    {
        var desktop = GetDesktopWindow();
        var parent = GetAncestor(hWnd, GA_PARENT);
        var winPos = HWND_NOTOPMOST;

        switch (pos)
        {
            case WINDOWZPOS.ONTOP:
                if ((GetWindowLong(hWnd, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0)
                {
                    return;
                }
                winPos = HWND_TOPMOST;
                break;

            case WINDOWZPOS.ONBOTTOM:
                winPos = HWND_BOTTOM;
                break;

            case WINDOWZPOS.ONDESKTOP:
                // Set the window's parent to progman, so it stays always on desktop
                var ProgmanHwnd = FindWindow("Progman", "Program Manager");
                if (ProgmanHwnd != IntPtr.Zero && parent == desktop)
                {
                    SetParent(hWnd, ProgmanHwnd);
                }
                else
                {
                    return; // The window is already on desktop
                }
                break;
        }

        if (pos != WINDOWZPOS.ONDESKTOP && parent != desktop)
        {
            SetParent(hWnd, IntPtr.Zero);
        }

        SetWindowPos(hWnd, winPos, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    #endregion
}
