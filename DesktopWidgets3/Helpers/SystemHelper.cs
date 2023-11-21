using System.Runtime.InteropServices;

namespace DesktopWidgets3.Helpers;

/// <summary>
/// Helper for actions related to windows system.
/// </summary>
public partial class SystemHelper
{
    /// <summary>
    /// Check if window exists and show window.
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    internal static partial IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

    public static bool IsWindowExist(string? className, string? windowName, bool showWindow)
    {
        var handle = FindWindow(className, windowName);
        if (handle != IntPtr.Zero)
        {
            if (showWindow)
            {
                const int SW_SHOW = 5;
                const int SW_RESTORE = 9;
                const int WM_SHOWWINDOW = 0x0018;
                const int SW_PARENTOPENING = 3;
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

    /// <summary>
    /// Show dialog.
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    public static int MessageBox(string lpText, string lpCaption, uint uType = 0)
    {
        return MessageBox(new IntPtr(0), lpText, lpCaption, uType);
    }

    /// <summary>
    /// Exit windows action.
    /// </summary>
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
}
