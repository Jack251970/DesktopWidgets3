using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using Vanara.PInvoke;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Windows.System;

namespace DesktopWidgets3.Helpers;

/// <summary>
/// Provides static helper for general Win32API.
/// </summary>
public class Win32API
{
    public static Task StartSTATask(Func<Task> func)
    {
        var taskCompletionSource = new TaskCompletionSource();
        var thread = new Thread(async () =>
        {
            Ole32.OleInitialize();

            try
            {
                await func();
                taskCompletionSource.SetResult();
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult();
            }
            finally
            {
                Ole32.OleUninitialize();
            }
        })

        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return taskCompletionSource.Task;
    }

    public static Task StartSTATask(Action action)
    {
        var taskCompletionSource = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            Ole32.OleInitialize();

            try
            {
                action();
                taskCompletionSource.SetResult();
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult();
            }
            finally
            {
                Ole32.OleUninitialize();
            }
        })

        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return taskCompletionSource.Task;
    }

    public static Task<T?> StartSTATask<T>(Func<T> func)
    {
        var taskCompletionSource = new TaskCompletionSource<T?>();

        var thread = new Thread(() =>
        {
            Ole32.OleInitialize();

            try
            {
                taskCompletionSource.SetResult(func());
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(default);
                //tcs.SetException(e);
            }
            finally
            {
                Ole32.OleUninitialize();
            }
        })

        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return taskCompletionSource.Task;
    }

    public static Task<T?> StartSTATask<T>(Func<Task<T>> func)
    {
        var taskCompletionSource = new TaskCompletionSource<T?>();

        var thread = new Thread(async () =>
        {
            Ole32.OleInitialize();
            try
            {
                taskCompletionSource.SetResult(await func());
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(default);
                //tcs.SetException(e);
            }
            finally
            {
                Ole32.OleUninitialize();
            }
        })

        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return taskCompletionSource.Task;
    }

    private class IconAndOverlayCacheEntry
    {
        public byte[]? Icon { get; set; }

        public byte[]? Overlay { get; set; }
    }

    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, IconAndOverlayCacheEntry>> _iconAndOverlayCache = new();

    private static readonly object _lock = new();

    public static (byte[]? icon, byte[]? overlay) GetFileIconAndOverlay(string path, int thumbnailSize, bool isFolder, bool getOverlay = true, bool onlyGetOverlay = false)
    {
        byte[]? iconData = null, overlayData = null;
        var entry = _iconAndOverlayCache.GetOrAdd(path, _ => new());

        if (entry.TryGetValue(thumbnailSize, out var cacheEntry))
        {
            iconData = cacheEntry.Icon;
            overlayData = cacheEntry.Overlay;

            if ((onlyGetOverlay && overlayData is not null) ||
                (!getOverlay && iconData is not null) ||
                (overlayData is not null && iconData is not null))
            {
                return (iconData, overlayData);
            }
        }

        try
        {
            if (!onlyGetOverlay)
            {
                using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPIDL(path);

                if (shellItem is not null && shellItem.IShellItem is Shell32.IShellItemImageFactory fctry)
                {
                    var flags = Shell32.SIIGBF.SIIGBF_BIGGERSIZEOK;
                    if (thumbnailSize < 80)
                    {
                        flags |= Shell32.SIIGBF.SIIGBF_ICONONLY;
                    }

                    var hres = fctry.GetImage(new SIZE(thumbnailSize, thumbnailSize), flags, out var hbitmap);
                    if (hres == HRESULT.S_OK)
                    {
                        using var image = GetBitmapFromHBitmap(hbitmap);
                        if (image is not null)
                        {
                            iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
                        }
                    }

                    //Marshal.ReleaseComObject(fctry);
                }
            }
            if (getOverlay || (!onlyGetOverlay && iconData is null))
            {
                var shfi = new Shell32.SHFILEINFO();
                var flags = Shell32.SHGFI.SHGFI_OVERLAYINDEX | Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_SYSICONINDEX | Shell32.SHGFI.SHGFI_ICONLOCATION;

                // Cannot access file, use file attributes
                var useFileAttibutes = !onlyGetOverlay && iconData is null;
                var ret = ShellFolderExtensions.GetStringAsPIDL(path, out var pidl) ?
                Shell32.SHGetFileInfo(pidl, 0, ref shfi, Shell32.SHFILEINFO.Size, Shell32.SHGFI.SHGFI_PIDL | flags) :
                Shell32.SHGetFileInfo(path, isFolder ? FileAttributes.Directory : 0, ref shfi, Shell32.SHFILEINFO.Size, flags | (useFileAttibutes ? Shell32.SHGFI.SHGFI_USEFILEATTRIBUTES : 0));
                if (ret == IntPtr.Zero)
                {
                    return (iconData, null);
                }

                User32.DestroyIcon(shfi.hIcon);

                var imageListSize = thumbnailSize switch
                {
                    <= 16 => Shell32.SHIL.SHIL_SMALL,
                    <= 32 => Shell32.SHIL.SHIL_LARGE,
                    <= 48 => Shell32.SHIL.SHIL_EXTRALARGE,
                    _ => Shell32.SHIL.SHIL_JUMBO,
                };

                lock (_lock)
                {
                    if (!Shell32.SHGetImageList(imageListSize, typeof(ComCtl32.IImageList).GUID, out var imageListOut).Succeeded)
                    {
                        return (iconData, null);
                    }

                    var imageList = (ComCtl32.IImageList)imageListOut;

                    if (!onlyGetOverlay && iconData is null)
                    {
                        var iconIdx = shfi.iIcon & 0xFFFFFF;
                        if (iconIdx != 0)
                        {
                            // Could not fetch thumbnail, load simple icon
                            using var hIcon = imageList.GetIcon(iconIdx, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);
                            if (!hIcon.IsNull && !hIcon.IsInvalid)
                            {
                                using var icon = hIcon.ToIcon();
                                using var image = icon.ToBitmap();
                                iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
                            }
                        }
                        else if (isFolder)
                        {
                            // Could not icon, load generic icon
                            var icons = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "imageres.dll"), new[] { 2 }, thumbnailSize);
                            var generic = icons.SingleOrDefault(x => x.Index == 2);
                            iconData = generic?.IconData;
                        }
                        else
                        {
                            // Could not icon, load generic icon
                            var icons = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll"), new[] { 1 }, thumbnailSize);
                            var generic = icons.SingleOrDefault(x => x.Index == 1);
                            iconData = generic?.IconData;
                        }
                    }

                    var overlayIdx = shfi.iIcon >> 24;
                    if (overlayIdx != 0 && getOverlay)
                    {
                        var overlayImage = imageList.GetOverlayImage(overlayIdx);
                        using var hOverlay = imageList.GetIcon(overlayImage, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);
                        if (!hOverlay.IsNull && !hOverlay.IsInvalid)
                        {
                            using var icon = hOverlay.ToIcon();
                            using var image = icon.ToBitmap();

                            overlayData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
                        }
                    }

                    Marshal.ReleaseComObject(imageList);
                }

                return (iconData, overlayData);
            }
            else
            {
                return (iconData, null);
            }
        }
        finally
        {
            cacheEntry = new IconAndOverlayCacheEntry();
            if (iconData is not null)
            {
                cacheEntry.Icon = iconData;
            }

            if (overlayData is not null)
            {
                cacheEntry.Overlay = overlayData;
            }

            entry[thumbnailSize] = cacheEntry;
        }
    }

    private static readonly ConcurrentDictionary<(string File, int Index, int Size), IconFileInfo> _iconCache = new();

    public static IList<IconFileInfo> ExtractSelectedIconsFromDLL(string file, IList<int> indexes, int iconSize = 48)
    {
        var iconsList = new List<IconFileInfo>();

        foreach (var index in indexes)
        {
            if (_iconCache.TryGetValue((file, index, iconSize), out var iconInfo))
            {
                iconsList.Add(iconInfo);
            }
            else
            {
                // This is merely to pass into the function and is unneeded otherwise
                if (Shell32.SHDefExtractIcon(file, -1 * index, 0, out var icon, out var hIcon2, Convert.ToUInt32(iconSize)) == HRESULT.S_OK)
                {
                    using var image = icon.ToBitmap();
                    var bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
                    iconInfo = new IconFileInfo(bitmapData, index);
                    _iconCache[(file, index, iconSize)] = iconInfo;
                    iconsList.Add(iconInfo);
                    User32.DestroyIcon(icon);
                    User32.DestroyIcon(hIcon2);
                }
            }
        }

        return iconsList;
    }

    public static Bitmap? GetBitmapFromHBitmap(HBITMAP hBitmap)
    {
        try
        {
            var bmp = Image.FromHbitmap((IntPtr)hBitmap);
            if (Image.GetPixelFormatSize(bmp.PixelFormat) < 32)
            {
                return bmp;
            }

            var bmBounds = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(bmBounds, ImageLockMode.ReadOnly, bmp.PixelFormat);

            if (IsAlphaBitmap(bmpData))
            {
                var alpha = GetAlphaBitmapFromBitmapData(bmpData);

                bmp.UnlockBits(bmpData);
                bmp.Dispose();

                return alpha;
            }

            bmp.UnlockBits(bmpData);

            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap GetAlphaBitmapFromBitmapData(BitmapData bmpData)
    {
        using var tmp = new Bitmap(bmpData.Width, bmpData.Height, bmpData.Stride, PixelFormat.Format32bppArgb, bmpData.Scan0);
        var clone = new Bitmap(tmp.Width, tmp.Height, tmp.PixelFormat);

        using (var gr = Graphics.FromImage(clone))
        {
            gr.DrawImage(tmp, new Rectangle(0, 0, clone.Width, clone.Height));
        }

        return clone;
    }

    private static bool IsAlphaBitmap(BitmapData bmpData)
    {
        for (var y = 0; y <= bmpData.Height - 1; y++)
        {
            for (var x = 0; x <= bmpData.Width - 1; x++)
            {
                var pixelColor = Color.FromArgb(Marshal.ReadInt32(bmpData.Scan0, (bmpData.Stride * y) + (4 * x)));

                if (pixelColor.A > 0 & pixelColor.A < 255)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public class IconFileInfo
    {
        public byte[] IconData
        {
            get;
        }

        public int Index
        {
            get;
        }

        public IconFileInfo(byte[] iconData, int index)
        {
            IconData = iconData;
            Index = index;
        }
    }

    public static IEnumerable<HWND> GetDesktopWindows()
    {
        var prevHwnd = HWND.NULL;
        var windowsList = new List<HWND>();

        while (true)
        {
            prevHwnd = User32.FindWindowEx(HWND.NULL, prevHwnd, null, null);
            if (prevHwnd == HWND.NULL)
            {
                break;
            }

            windowsList.Add(prevHwnd);
        }

        return windowsList;
    }

    public static Task<bool> MountVhdDisk(string vhdPath)
    {
        // Mounting requires elevation
        return RunPowershellCommandAsync($"-command \"Mount-DiskImage -ImagePath '{vhdPath}'\"", true);
    }

    public static async Task<bool> RunPowershellCommandAsync(string command, bool runAsAdmin)
    {
        using Process process = CreatePowershellProcess(command, runAsAdmin);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30 * 1000));

        try
        {
            process.Start();
            await process.WaitForExitAsync(cts.Token);
            return process.ExitCode == 0;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            // If user cancels UAC
            return false;
        }
    }

    private static Process CreatePowershellProcess(string command, bool runAsAdmin)
    {
        Process process = new();

        if (runAsAdmin)
        {
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Verb = "runas";
        }

        process.StartInfo.FileName = "powershell.exe";
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        process.StartInfo.Arguments = command;

        return process;
    }

    public static void BringToForeground(IEnumerable<HWND> currentWindows)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(5 * 1000);

        Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(500);

                var newWindows = GetDesktopWindows().Except(currentWindows).Where(x => User32.IsWindowVisible(x) && !User32.IsIconic(x));
                if (newWindows.Any())
                {
                    foreach (var newWindow in newWindows)
                    {
                        User32.SetWindowPos(
                            newWindow,
                            User32.SpecialWindowHandles.HWND_TOPMOST,
                            0, 0, 0, 0,
                            User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);

                        User32.SetWindowPos(
                            newWindow,
                            User32.SpecialWindowHandles.HWND_NOTOPMOST,
                            0, 0, 0, 0,
                            User32.SetWindowPosFlags.SWP_SHOWWINDOW | User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);
                    }

                    break;
                }
            }
        });
    }

    public static async Task<string?> GetFileAssociationAsync(string filename, bool checkDesktopFirst = false)
    {
        // Find UWP apps
        async Task<string?> GetUwpAssoc()
        {
            var uwpApps = await Launcher.FindFileHandlersAsync(Path.GetExtension(filename));
            return uwpApps.Any() ? uwpApps[0].PackageFamilyName : null;
        }

        // Find desktop apps
        string? GetDesktopAssoc()
        {
            var lpResult = new StringBuilder(2048);
            var hResult = Shell32.FindExecutable(filename, null, lpResult);

            return hResult.ToInt64() > 32 ? lpResult.ToString() : null;
        }

        if (checkDesktopFirst)
            return GetDesktopAssoc() ?? await GetUwpAssoc();

        return await GetUwpAssoc() ?? GetDesktopAssoc();
    }

}
