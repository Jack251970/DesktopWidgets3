// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Core.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using static Files.App.Utils.Shell.Win32API;

namespace Files.App.Helpers;

public static class UIHelpers
{
    #region icon resource

    private static readonly IconFileInfo ShieldIconResource = LoadShieldIconResource();

    public static async Task<BitmapImage?> GetShieldIconResource()
    {
        return ShieldIconResource is not null
            ? await ShieldIconResource.IconData.ToBitmapAsync()
            : null;
    }

    private static IconFileInfo LoadShieldIconResource()
    {
        var imageres = Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");
        var imageResList = ExtractSelectedIconsFromDLL(imageres, 
            new List<int>() { Constants.ImageRes.ShieldIcon }, 16);

        return imageResList.First();
    }

    #endregion

    #region dialog
    
    public static async Task<DialogResult> TryShowAsync<TViewModel>(this IDialog<TViewModel> dialog, FolderViewViewModel folderViewModel)
            where TViewModel : class, INotifyPropertyChanged
    {
        return (DialogResult)await ((ContentDialog)dialog).TryShowAsync(folderViewModel);
    }

    public static async Task<ContentDialogResult> TryShowAsync(this ContentDialog dialog, FolderViewViewModel folderViewModel)
    {
        if (!folderViewModel.CanShowDialog)
        {
            return ContentDialogResult.None;
        }

        try
        {
            folderViewModel.CanShowDialog = false;
            return await SetContentDialogRoot(dialog, folderViewModel).ShowAsync();
        }
        catch // A content dialog is already open
        {
            return ContentDialogResult.None;
        }
        finally
        {
            folderViewModel.CanShowDialog = true;
        }
    }

    // WINUI3
    private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog, FolderViewViewModel folderViewModel)
    {
        if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            contentDialog.XamlRoot = folderViewModel.WidgetWindow.Content.XamlRoot;
        }
        return contentDialog;
    }

    #endregion
}