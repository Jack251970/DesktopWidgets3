using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopWidgets3.Core.Helpers;

/// <summary>
/// Helper for actions related to windows system.
/// </summary>
public partial class SystemHelper
{
    #region check window existence

    /// <summary>
    /// Check if window exists and show window.
    /// </summary>
    public static bool IsWindowExist(string? className, string? windowName, bool showWindow)
    {
        var hwnd = PInvoke.FindWindow(className, windowName);
        if (hwnd != IntPtr.Zero)
        {
            if (showWindow)
            {
                // show window
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOW);
                PInvoke.SendMessage(hwnd, PInvoke.WM_SHOWWINDOW, 0, new LPARAM((nint)SHOW_WINDOW_STATUS.SW_PARENTOPENING));

                // bring window to front
                PInvoke.SetForegroundWindow(hwnd);
            }
            return true;
        }
        return false;
    }

    #endregion

    #region window z position

    public enum WINDOWZPOS
    {
        ONTOP,
        ONBOTTOM,
        ONDESKTOP
    }

    /// <summary>
    /// Set window Z position.
    /// </summary>
    public static void SetWindowZPos(IntPtr hwnd, WINDOWZPOS pos)
    {
        var fHwnd = new HWND(hwnd);
        var fZero = new HWND(IntPtr.Zero);
        var desktop = PInvoke.GetDesktopWindow();
        var parent = PInvoke.GetAncestor(fHwnd, GET_ANCESTOR_FLAGS.GA_PARENT);
        var winPos = HWND.HWND_NOTOPMOST;

        switch (pos)
        {
            case WINDOWZPOS.ONTOP:
                if ((PInvoke.GetWindowLong(fHwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE) & (uint)WINDOW_EX_STYLE.WS_EX_TOPMOST) != 0)
                {
                    return;
                }
                winPos = HWND.HWND_TOPMOST;
                break;

            case WINDOWZPOS.ONBOTTOM:
                winPos = HWND.HWND_BOTTOM;
                break;

            case WINDOWZPOS.ONDESKTOP:
                // Set the window's parent to progman, so it stays always on desktop
                var progmanHwnd = PInvoke.FindWindow("Progman", "Program Manager");
                if (progmanHwnd != fZero && parent == desktop)
                {
                    PInvoke.SetParent(fHwnd, progmanHwnd);
                }
                else
                {
                    return; // The window is already on desktop
                }
                break;
        }

        if (pos != WINDOWZPOS.ONDESKTOP && parent != desktop)
        {
            PInvoke.SetParent(fHwnd, fZero);
        }

        PInvoke.SetWindowPos(fHwnd, new(winPos), 0, 0, 0, 0, 
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    /// <summary>
    /// Force window to stay at bottom of other upper windows.
    /// </summary>
    public static void ForceWindowPosition(nint lParam)
    {
        var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
        windowPos.flags |= SET_WINDOW_POS_FLAGS.SWP_NOZORDER;
        Marshal.StructureToPtr(windowPos, lParam, false);
    }

    #endregion
}
