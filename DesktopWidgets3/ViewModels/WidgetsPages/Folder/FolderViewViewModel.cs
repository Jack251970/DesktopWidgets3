using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.ViewModels.WidgetsPages.Folder;

public partial class FolderViewViewModel : ObservableRecipient
{
    private string folderPath = $"C:\\Users\\11602\\Downloads";

    [ObservableProperty]
    private string _FolderName = string.Empty;

    public ObservableCollection<FolderViewFileItem> FolderViewFileItems { get; set; } = new();

    public FolderViewViewModel()
    {
        LoadFileItemsFromFolderPath();
    }

    internal void FolderViewItemClick(object sender)
    {
        var button = sender as Button;
        if (button == null)
        {
            return;
        }

        if (button.Tag is not string filePath)
        {
            return;
        }
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
        {
            folderPath = filePath;
            LoadFileItemsFromFolderPath();
        }
        else
        {
            if (File.Exists(filePath))
            {
                // 打开文件
            }
        }
    }

    private async void LoadFileItemsFromFolderPath()
    {
        FolderName = Path.GetFileName(folderPath);

        FolderViewFileItems.Clear();

        foreach (var directory in Directory.GetDirectories(folderPath))
        {
            var folderName = Path.GetFileName(directory);
            var folderPath = directory;
            FolderViewFileItems.Add(new FolderViewFileItem()
            {
                FileName = folderName,
                FilePath = folderPath,
                FileIcon = await GetFileIcon(folderPath, true),
            });
        }

        foreach (var file in Directory.GetFiles(folderPath))
        {
            var fileName = Path.GetFileName(file);
            var filePath = file;
            FolderViewFileItems.Add(new FolderViewFileItem()
            {
                FileName = fileName,
                FilePath = filePath,
                FileIcon = await GetFileIcon(filePath, false),
            });
        }
    }

    private static async Task<BitmapImage?> GetFileIcon(string filePath, bool isFolder)
    {
        var (iconData, _) = await FileIconHelper.LoadIconAndOverlayAsync(filePath, 96, isFolder);

        return await iconData.ToBitmapAsync();

        /*if (iconInfo.OverlayData is not null)
        {
            await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
            {
                item.IconOverlay = await iconInfo.OverlayData.ToBitmapAsync();
                item.ShieldIcon = await GetShieldIcon();
            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
        }*/
    }
}
