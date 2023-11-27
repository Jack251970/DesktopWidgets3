﻿using System.Drawing;
using System.Runtime.InteropServices;
using DesktopWidgets3.Helpers;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

/// <summary>
/// Provides a helper for Win32 context menu.
/// </summary>
public class ContextMenu : Win32ContextMenu, IDisposable
{
    private Shell32.IContextMenu _cMenu;

    private User32.SafeHMENU? _hMenu;

    private readonly ThreadWithMessageQueue _owningThread;

    private readonly Func<string, bool>? _itemFilter;

    private readonly Dictionary<List<Win32ContextMenuItem>, Action> _loadSubMenuActions;

    // To detect redundant calls
    private bool disposedValue = false;

    public List<string> ItemsPath
    {
        get;
    }

    private ContextMenu(Shell32.IContextMenu cMenu, User32.SafeHMENU hMenu, IEnumerable<string> itemsPath, ThreadWithMessageQueue owningThread, Func<string, bool>? itemFilter)
    {
        _cMenu = cMenu;
        _hMenu = hMenu;
        _owningThread = owningThread;
        _itemFilter = itemFilter;
        _loadSubMenuActions = new();

        ItemsPath = itemsPath.ToList();
        Items = new();
    }

    public static async Task<bool> InvokeVerb(string verb, params string[] filePaths)
    {
        using var cMenu = await GetContextMenuForFiles(filePaths, Shell32.CMF.CMF_DEFAULTONLY);

        return cMenu is not null && await cMenu.InvokeVerb(verb);
    }

    public async Task<bool> InvokeVerb(string? verb)
    {
        if (string.IsNullOrEmpty(verb) || Items is null)
        {
            return false;
        }

        var item = Items.FirstOrDefault(x => x.CommandString == verb);
        if (item is not null && item.ID >= 0)
        {
            // Prefer invocation by ID
            return await InvokeItem(item.ID);
        }

        try
        {
            var currentWindows = Win32API.GetDesktopWindows();

            var pici = new Shell32.CMINVOKECOMMANDINFOEX
            {
                lpVerb = new SafeResourceId(verb, CharSet.Ansi),
                nShow = ShowWindowCommand.SW_SHOWNORMAL,
            };

            pici.cbSize = (uint)Marshal.SizeOf(pici);

            await _owningThread.PostMethod(() => _cMenu.InvokeCommand(pici));
            Win32API.BringToForeground(currentWindows);

            return true;
        }
        catch (Exception)
        {

        }

        return false;
    }

    public async Task<bool> InvokeItem(int itemID)
    {
        if (itemID < 0)
        {
            return false;
        }

        try
        {
            var currentWindows = Win32API.GetDesktopWindows();
            var pici = new Shell32.CMINVOKECOMMANDINFOEX
            {
                lpVerb = Macros.MAKEINTRESOURCE(itemID),
                nShow = ShowWindowCommand.SW_SHOWNORMAL,
            };

            pici.cbSize = (uint)Marshal.SizeOf(pici);

            await _owningThread.PostMethod(() => _cMenu.InvokeCommand(pici));
            Win32API.BringToForeground(currentWindows);

            return true;
        }
        catch (Exception)
        {

        }

        return false;
    }

    public static async Task<ContextMenu?> GetContextMenuForFiles(string[] filePathList, Shell32.CMF flags, Func<string, bool>? itemFilter = null)
    {
        var owningThread = new ThreadWithMessageQueue();

        return await owningThread.PostMethod<ContextMenu>(() =>
        {
            var shellItems = new List<ShellItem>();

            try
            {
                foreach (var filePathItem in filePathList.Where(x => !string.IsNullOrEmpty(x)))
                {
                    shellItems.Add(ShellFolderExtensions.GetShellItemFromPathOrPIDL(filePathItem));
                }

                return GetContextMenuForFiles(shellItems.ToArray(), flags, owningThread, itemFilter);
            }
            catch
            {
                // Return empty context menu
                return null;
            }
            finally
            {
                foreach (var item in shellItems)
                {
                    item.Dispose();
                }
            }
        });
    }

    public static async Task<ContextMenu?> GetContextMenuForFiles(ShellItem[] shellItems, Shell32.CMF flags, Func<string, bool>? itemFilter = null)
    {
        var owningThread = new ThreadWithMessageQueue();

        return await owningThread.PostMethod<ContextMenu>(() => GetContextMenuForFiles(shellItems, flags, owningThread, itemFilter));
    }

    private static ContextMenu? GetContextMenuForFiles(ShellItem[] shellItems, Shell32.CMF flags, ThreadWithMessageQueue owningThread, Func<string, bool>? itemFilter = null)
    {
        if (!shellItems.Any())
        {
            return null;
        }

        try
        {
            // NOTE: The items are all in the same folder
            using var sf = shellItems[0].Parent;

            var menu = sf.GetChildrenUIObjects<Shell32.IContextMenu>(default, shellItems);
            var hMenu = User32.CreatePopupMenu();
            menu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, flags);
            var contextMenu = new ContextMenu(menu, hMenu, shellItems.Select(x => x.ParsingName), owningThread, itemFilter);
            contextMenu.EnumMenuItems(hMenu, contextMenu.Items);

            return contextMenu;
        }
        catch (COMException)
        {
            // Return empty context menu
            return null;
        }
    }

    public static async Task WarmUpQueryContextMenuAsync()
    {
        using var cMenu = await GetContextMenuForFiles(new string[] { "C:\\" }, Shell32.CMF.CMF_NORMAL);
    }

    private void EnumMenuItems(HMENU hMenu, List<Win32ContextMenuItem> menuItemsResult, bool loadSubenus = false)
    {
        var itemCount = User32.GetMenuItemCount(hMenu);

        var menuItemInfo = new User32.MENUITEMINFO()
        {
            fMask =
                User32.MenuItemInfoMask.MIIM_BITMAP |
                User32.MenuItemInfoMask.MIIM_FTYPE |
                User32.MenuItemInfoMask.MIIM_STRING |
                User32.MenuItemInfoMask.MIIM_ID |
                User32.MenuItemInfoMask.MIIM_SUBMENU,
        };

        menuItemInfo.cbSize = (uint)Marshal.SizeOf(menuItemInfo);

        for (uint index = 0; index < itemCount; index++)
        {
            var menuItem = new ContextMenuItem();
            var container = new SafeCoTaskMemString(512);
            var cMenu2 = _cMenu as Shell32.IContextMenu2;

            menuItemInfo.dwTypeData = (IntPtr)container;

            // See also, https://devblogs.microsoft.com/oldnewthing/20040928-00/?p=37723
            menuItemInfo.cch = (uint)container.Capacity - 1;

            var result = User32.GetMenuItemInfo(hMenu, index, true, ref menuItemInfo);
            if (!result)
            {
                container.Dispose();
                continue;
            }

            menuItem.Type = (MenuItemType)menuItemInfo.fType;

            // wID - idCmdFirst
            menuItem.ID = (int)(menuItemInfo.wID - 1);

            if (menuItem.Type == MenuItemType.MFT_STRING)
            {
                menuItem.Label = menuItemInfo.dwTypeData;
                menuItem.CommandString = GetCommandString(_cMenu, menuItemInfo.wID - 1);

                if (_itemFilter is not null && (_itemFilter(menuItem.CommandString) || _itemFilter(menuItem.Label)))
                {
                    // Skip items implemented in UWP
                    container.Dispose();
                    continue;
                }

                if (menuItemInfo.hbmpItem != HBITMAP.NULL && !Enum.IsDefined(typeof(HBITMAP_HMENU), ((IntPtr)menuItemInfo.hbmpItem).ToInt64()))
                {
                    using var bitmap = Win32API.GetBitmapFromHBitmap(menuItemInfo.hbmpItem);

                    if (bitmap is not null)
                    {
                        // Make the icon background transparent
                        bitmap.MakeTransparent();

                        var bitmapData = (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]));
                        menuItem.Icon = bitmapData;
                    }
                }

                if (menuItemInfo.hSubMenu != HMENU.NULL)
                {
                    var subItems = new List<Win32ContextMenuItem>();
                    var hSubMenu = menuItemInfo.hSubMenu;

                    if (loadSubenus)
                    {
                        LoadSubMenu();
                    }
                    else
                    {
                        _loadSubMenuActions.Add(subItems, LoadSubMenu);
                    }

                    menuItem.SubItems = subItems;

                    void LoadSubMenu()
                    {
                        try
                        {
                            cMenu2?.HandleMenuMsg((uint)User32.WindowMessage.WM_INITMENUPOPUP, (IntPtr)hSubMenu, new IntPtr(index));
                        }
                        catch (Exception ex) when (ex is COMException or NotImplementedException)
                        {
                            // Only for dynamic/owner drawn? (open with, etc)
                        }

                        EnumMenuItems(hSubMenu, subItems, true);
                    }
                }
            }

            container.Dispose();
            menuItemsResult.Add(menuItem);
        }
    }

    public Task<bool> LoadSubMenu(List<Win32ContextMenuItem> subItems)
    {
        if (_loadSubMenuActions.Remove(subItems, out var loadSubMenuAction))
        {
            return _owningThread.PostMethod<bool>(() =>
            {
                try
                {
                    loadSubMenuAction!();
                    return true;
                }
                catch (COMException)
                {
                    return false;
                }
            });
        }
        else
        {
            return Task.FromResult(false);
        }
    }

    private static string? GetCommandString(Shell32.IContextMenu cMenu, uint offset, Shell32.GCS flags = Shell32.GCS.GCS_VERBW)
    {
        // A workaround to avoid an AccessViolationException on some items,
        // notably the "Run with graphic processor" menu item of NVIDIA cards
        if (offset > 5000)
        {
            return null;
        }

        SafeCoTaskMemString? commandString = null;

        try
        {
            commandString = new SafeCoTaskMemString(512);
            cMenu.GetCommandString(new IntPtr(offset), flags, IntPtr.Zero, commandString, (uint)commandString.Capacity - 1);

            return commandString.ToString();
        }
        catch (Exception ex) when (ex is InvalidCastException or ArgumentException)
        {
            return null;
        }
        catch (Exception ex) when (ex is COMException or NotImplementedException)
        {
            // Not every item has an associated verb
            return null;
        }
        finally
        {
            commandString?.Dispose();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: Dispose managed state (managed objects)
                if (Items is not null)
                {
                    foreach (var si in Items)
                    {
                        (si as IDisposable)?.Dispose();
                    }

                    Items = null;
                }
            }

            // TODO: Free unmanaged resources (unmanaged objects) and override a finalizer below
            if (_hMenu is not null)
            {
                User32.DestroyMenu(_hMenu);
                _hMenu = null;
            }
            if (_cMenu is not null)
            {
                Marshal.ReleaseComObject(_cMenu);
                _cMenu = null;
            }

            _owningThread.Dispose();

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ContextMenu()
    {
        Dispose(false);
    }
}

public enum MenuItemType : uint
{
    MFT_STRING = 0,
    MFT_BITMAP = 4,
    MFT_MENUBARBREAK = 32,
    MFT_MENUBREAK = 64,
    MFT_OWNERDRAW = 256,
    MFT_RADIOCHECK = 512,
    MFT_SEPARATOR = 2048,
    MFT_RIGHTORDER = 8192,
    MFT_RIGHTJUSTIFY = 16384
}

public enum HBITMAP_HMENU : long
{
    HBMMENU_CALLBACK = -1,
    HBMMENU_MBAR_CLOSE = 5,
    HBMMENU_MBAR_CLOSE_D = 6,
    HBMMENU_MBAR_MINIMIZE = 3,
    HBMMENU_MBAR_MINIMIZE_D = 7,
    HBMMENU_MBAR_RESTORE = 2,
    HBMMENU_POPUP_CLOSE = 8,
    HBMMENU_POPUP_MAXIMIZE = 10,
    HBMMENU_POPUP_MINIMIZE = 11,
    HBMMENU_POPUP_RESTORE = 9,
    HBMMENU_SYSTEM = 1
}

public class Win32ContextMenu
{
    public List<Win32ContextMenuItem>? Items
    {
        get; set;
    }
}

public class Win32ContextMenuItem
{
    public byte[]? Icon
    {
        get; set;
    }
    public int ID
    {
        get; set;
    } // Valid only in current menu to invoke item
    public string? Label
    {
        get; set;
    }
    public string? CommandString
    {
        get; set;
    }
    public MenuItemType Type
    {
        get; set;
    }
    public List<Win32ContextMenuItem>? SubItems
    {
        get; set;
    }
}

public class ContextMenuItem : Win32ContextMenuItem, IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && SubItems is not null)
        {
            foreach (var subItem in SubItems)
                (subItem as IDisposable)?.Dispose();

            SubItems = null;
        }
    }
}