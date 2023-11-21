using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IWshRuntimeLibrary;
using Microsoft.UI.Xaml.Media.Imaging;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models;
using Windows.Storage.FileProperties;
using Windows.Storage;
using DesktopWidgets3.Helpers;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class BlockListViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty]
    private string _changeListContent = "BlockList_ChangeList_ToSystemApps".GetLocalized();

    #region Const Strings

    private readonly string[] StartMenuSoftwarePaths =
    {
        "C:\\Users\\0\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs".Replace("0", Environment.UserName.ToString()),
        "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs",
    };

    private readonly string[] SystemFoldNames =
    {
        "Accessibility",        // Windows Ease of Access
        "Accessories",          // Windows Accessories
        "Administrative Tools", // Windows Administrative Tools
        "System Tools",         // Windows System
        "Windows PowerShell"    // Windows PowerShell
    };

    private readonly string[] SystemExeNames =
    {
        "Control.exe", "control.exe",   // Control Panel
        
    };

    private readonly string[] ExcludeExeNames =
    {
        "DesktopWidgets3.exe"      // self
    };

    #endregion

    private readonly IAppSettingsService _appSettingsService;

    private bool _isInitialized = false;

    public ObservableCollection<AppInfo> AppInfoItems { get; set; } = new();

    private readonly List<AppInfo> SystemApps = new();
    private readonly List<AppInfo> UserApps = new();

    private bool _showUserApps = true;

    public BlockListViewModel(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            LoadStartMenuAppItemTexts();
            await LoadStartMenuAppItems();
        }
    }

    public void OnNavigatedFrom()
    {
        
    }

    [RelayCommand]
    private void OnChangeList()
    {
        _showUserApps = !_showUserApps;
        ChangeListContent = _showUserApps ? "BlockList_ChangeList_ToSystemApps".GetLocalized() : "BlockList_ChangeList_ToUserApps".GetLocalized();
        _ = LoadStartMenuAppItems();
    }

    public void OnFilterAppsTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = ((TextBox)sender).Text;
        var allApps = _showUserApps ? UserApps : SystemApps;
        var filteredApps = allApps.Where(appInfo => FilterItem(appInfo, searchText));
        RemoveNonMatchingItems(filteredApps);
        AddBackNotContainItems(filteredApps);
    }

    public void OnListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_appSettingsService.IsLocking)
        {
            return;
        }
        var index = ((ListView)sender).SelectedIndex;
        if (index >= 0)
        {
            var item = AppInfoItems[index];
            var isBlock = !item.IsBlock;
            var exeName = item.ExeName;
            var listIndex = item.ListIndex;
            AppInfoItems.Remove(item);
            item.IsBlock = isBlock;
            AppInfoItems.Insert(index, item);
            ((ListView)sender).SelectedIndex = -1;
            _appSettingsService.SaveBlockList(exeName, isBlock);
            (_showUserApps ? UserApps : SystemApps)[listIndex].IsBlock = isBlock;
        }
    }

    private static bool FilterItem(AppInfo appInfo, string searchText)
    {
        return appInfo.AppName.Contains(searchText, StringComparison.InvariantCultureIgnoreCase);
    }

    private void RemoveNonMatchingItems(IEnumerable<AppInfo> filteredApps)
    {
        var filteredExeNames = filteredApps.Select(app => app.ExeName).ToList();
        for (var i = AppInfoItems.Count - 1; i >= 0; i--)
        {
            var item = AppInfoItems[i];
            if (!filteredExeNames.Contains(item.AppName))
            {
                AppInfoItems.Remove(item);
            }
        }
    }

    private void AddBackNotContainItems(IEnumerable<AppInfo> filteredApps)
    {
        foreach (var item in filteredApps)
        {
            if (!AppInfoItems.Contains(item))
            {
                AppInfoItems.Add(item);
                item.AppIcon ??= GetExeIconAsync(item.AppPath).Result;
            }
        }
    }

    private void LoadStartMenuAppItemTexts()
    {
        var userAppPathList = new List<string>();
        var systemAppPathList = new List<string>();
        var blockList = _appSettingsService.GetBlockList();

        foreach (var path in StartMenuSoftwarePaths)
        {
            var inkFiles = Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories);

            // Get ink files in previous folder without "\Programs"
            var addPath = path[..^9];
            var addInkFiles = Directory.GetFiles(addPath, "*.lnk", SearchOption.TopDirectoryOnly);

            inkFiles = inkFiles.Concat(addInkFiles).ToArray();

            foreach (var inkFile in inkFiles)
            {
                // skip uninstallers (only support Chinese & English)
                if (inkFile.Contains("卸载") | inkFile.ToLower().Contains("uninstall"))
                {
                    continue;
                }

                // handle ink files
                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(inkFile);
                var realPath = shortcut.TargetPath;

                // get real names
                var temp = realPath.Split("\\".ToCharArray());
                var exeName = temp[^1];

                // check real paths and real names
                if (ExcludeExeNames.Contains(exeName) | realPath.Length < 2 | !realPath.EndsWith(".exe"))
                {
                    continue;
                }

                // get ink file names
                temp = inkFile.Split("\\".ToCharArray());
                var inkName = temp[^1];
                inkName = inkName[..^4];

                // get names of previous folders
                var folderName = temp[^2];

                // get icon paths and block statues
                var isBlock = blockList.Contains(exeName);

                // distinguish between system apps and user apps
                if (SystemExeNames.Contains(exeName) | SystemFoldNames.Contains(folderName))
                {
                    if (!systemAppPathList.Contains(realPath))
                    {
                        var item = new AppInfo() { AppPath = realPath, AppName = inkName, IsBlock = isBlock, ExeName = exeName, ListIndex = systemAppPathList.Count };
                        systemAppPathList.Add(realPath);
                        SystemApps.Add(item);
                    }
                }
                else
                {
                    if (!userAppPathList.Contains(realPath))
                    {
                        var item = new AppInfo() { AppPath = realPath, AppName = inkName, IsBlock = isBlock, ExeName = exeName, ListIndex = userAppPathList.Count };
                        userAppPathList.Add(realPath);
                        UserApps.Add(item);
                    }
                }
            }
        }
    }

    private async Task LoadStartMenuAppItems()
    {
        AppInfoItems.Clear();
        foreach (var item in _showUserApps ? UserApps : SystemApps)
        {
            item.AppIcon ??= await GetExeIconAsync(item.AppPath);
            AppInfoItems.Add(item);
        }
    }

    private static async Task<BitmapImage?> GetExeIconAsync(string FilePath)
    {
        // Create a StorageFile object from the file path
        var file = await StorageFile.GetFileFromPathAsync(FilePath);
        if (file != null)
        {
            // Get the icon of the exe file as a bitmap image
            var icon = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(icon);
            // Dispose the icon to release the resources
            icon.Dispose();
            return bitmap;
        }
        return null;
    }
}
