using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.Core.Data.Items;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.ViewModels.Pages.Widget.FolderView;

public partial class FolderViewViewModel : ObservableRecipient, INavigationAware
{
    private readonly Stack<string> navigationFolderPaths = new();

    private string folderPath = string.Empty;
    private string FolderPath
    {
        get => folderPath;
        set
        {
            if (folderPath != value)
            {
                folderPath = value;
                parentFolderPath = Path.GetDirectoryName(FolderPath);
                fileSystemWatcher.Path = value;
            }
        }
    }
    private string? parentFolderPath;
    private readonly FileSystemWatcher fileSystemWatcher = new();

    [ObservableProperty]
    private string _FolderName = string.Empty;

    [ObservableProperty]
    private bool _isNavigateBackExecutable = false;

    [ObservableProperty]
    private bool _isNavigateUpExecutable = false;

    [ObservableProperty]
    private BitmapImage? _folderPathIcon = null;

    public ObservableCollection<FolderViewFileItem> FolderViewFileItems { get; set; } = new();

    public FolderViewViewModel()
    {
        
    }

    public void OnNavigatedTo(object parameter)
    {
        FolderPath = $"C:\\Users\\11602\\OneDrive\\文档\\My-Data";
        _ = LoadFileItemsFromFolderPath(true, null);
    }

    public void OnNavigatedFrom()
    {
        
    }

    internal async Task FolderViewItemDoubleTapped(string filePath)
    {
        var isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(filePath);
        if (isShortcut)
        {
            var shortcutInfo = new ShellLinkItem();
            var shInfo = await FileOperationsHelpers.ParseLinkAsync(filePath);
            if (shInfo is null || shInfo.TargetPath is null || shortcutInfo.InvalidTarget)
            {
                return;
            }

            filePath = shInfo.TargetPath;
        }

        var isDirectory = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Directory);
        if (isDirectory)
        {
            FolderPath = filePath;
            BitmapImage? folderPathIcon = null;
            foreach (var item in FolderViewFileItems)
            {
                if (item.FilePath == filePath)
                {
                    folderPathIcon = item.FileIcon;
                    break;
                }
            }
            await LoadFileItemsFromFolderPath(true, folderPathIcon);
        }
        else
        {
            if (!File.Exists(filePath))
            {
                await LoadFileItemsFromFolderPath(false, FolderPathIcon);
            }
            else
            {
                await OpenFileHelper.OpenPath(filePath, string.Empty, FolderPath);
            }
        }
    }

    internal async Task NavigateBackButtonClick()
    {
        if (IsNavigateBackExecutable)
        {
            navigationFolderPaths.Pop();
            FolderPath = navigationFolderPaths.Peek();
            await LoadFileItemsFromFolderPath(false, null);
        }
    }

    internal async Task NavigateUpButtonClick()
    {
        if (IsNavigateUpExecutable)
        {
            FolderPath = parentFolderPath!;
            await LoadFileItemsFromFolderPath(true, null);
        }
    }

    internal async Task NavigateRefreshButtonClick()
    {
        await LoadFileItemsFromFolderPath(false, FolderPathIcon);
    }

    private async Task LoadFileItemsFromFolderPath(bool pushFolderPath, BitmapImage? icon)
    {
        if (DiskRegex().IsMatch(FolderPath))
        {
            FolderName = FolderPath[..1];
        }
        else
        {
            FolderName = Path.GetFileName(FolderPath);
        }

        if (pushFolderPath)
        {
            navigationFolderPaths.Push(FolderPath);
        }
        IsNavigateBackExecutable = navigationFolderPaths.Count > 1;
        IsNavigateUpExecutable = parentFolderPath != null;

        FolderViewFileItems.Clear();

        var directories = Directory.GetDirectories(FolderPath);
        foreach (var directory in directories)
        {
            var directoryPath = directory;
            var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(directoryPath, FileAttributes.Hidden);
            if (!isHiddenItem)
            {
                var folderName = Path.GetFileName(directory);
                var (fileIcon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(directoryPath, true);
                FolderViewFileItems.Add(new FolderViewFileItem()
                {
                    FileName = folderName,
                    FilePath = directoryPath,
                    FileIcon = fileIcon,
                });
            }
        }

        var files = Directory.GetFiles(FolderPath);
        foreach (var file in files)
        {
            var filePath = file;
            var isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(filePath, FileAttributes.Hidden);
            if (!isHiddenItem)
            {
                var fileName = Path.GetFileName(file);
                var (fileIcon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(filePath, false);
                FolderViewFileItems.Add(new FolderViewFileItem()
                {
                    FileName = fileName,
                    FilePath = filePath,
                    FileIcon = fileIcon,
                });
            }
        }

        // TODO: fix icon loading bug
        if (icon is null)
        {
            (icon, _) = await FileIconHelper.GetFileIconAndOverlayAsync(FolderPath, true);
        }
        FolderPathIcon = icon;
    }

    [GeneratedRegex("^[a-zA-Z]:\\\\$")]
    private static partial Regex DiskRegex();
}
