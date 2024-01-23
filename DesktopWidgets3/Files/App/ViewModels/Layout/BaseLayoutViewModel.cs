// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.Core.Data.Items;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;

namespace Files.App.ViewModels.Layouts;

/// <summary>
/// Represents ViewModel for <see cref="BaseLayoutPage"/>.
/// </summary>
public class BaseLayoutViewModel : IDisposable
{
    private readonly FolderViewViewModel viewModel;

    public ICommand CreateNewFileCommand { get; private set; }

    public ICommand DragOverCommand { get; private set; }

    public ICommand DropCommand { get; private set; }

    public BaseLayoutViewModel(FolderViewViewModel viewModel)
    {
        this.viewModel = viewModel;

        CreateNewFileCommand = new RelayCommand<ShellNewEntry>(CreateNewFile!);
        DragOverCommand = new AsyncRelayCommand<DragEventArgs>(DragOverAsync!);
        DropCommand = new AsyncRelayCommand<DragEventArgs>(DropAsync!);
    }

    private async void CreateNewFile(ShellNewEntry f)
    {
        // await UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(AddItemDialogItemType.File, f);//_associatedInstance);
    }

    public async Task DragOverAsync(DragEventArgs e)
    {
        var deferral = e.GetDeferral();

        if (viewModel.InstanceViewModel.IsPageTypeSearchResults)
        {
            e.AcceptedOperation = DataPackageOperation.None;
            deferral.Complete();
            return;
        }

        viewModel.ItemManipulationModel.ClearSelection();

        if (FileSystemHelpers.HasDraggedStorageItems(e.DataView))
        {
            e.Handled = true;

            var draggedItems = await FileSystemHelpers.GetDraggedStorageItems(e.DataView);

            var pwd = viewModel.FileSystemViewModel.WorkingDirectory.TrimPath();
            var folderName = Path.IsPathRooted(pwd) && Path.GetPathRoot(pwd) == pwd ? Path.GetPathRoot(pwd) : Path.GetFileName(pwd);

            // As long as one file doesn't already belong to this folder
            if (viewModel.InstanceViewModel.IsPageTypeSearchResults || draggedItems.Any() && draggedItems.AreItemsAlreadyInFolder(viewModel.FileSystemViewModel.WorkingDirectory))
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            else if (!draggedItems.Any())
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = true;
                if (pwd!.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
                {
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
                {
                    e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Link;
                }
                else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
                {
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
                else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
                {
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else if (draggedItems.Any(x =>
                    x.Item is ZipStorageFile ||
                    x.Item is ZipStorageFolder) ||
                    ZipStorageFolder.IsZipPath(pwd))
                {
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
                else if (draggedItems.AreItemsInSameDrive(viewModel.FileSystemViewModel.WorkingDirectory))
                {
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
        }

        deferral.Complete();
    }

    public async Task DropAsync(DragEventArgs e)
    {
        e.Handled = true;

        if (FileSystemHelpers.HasDraggedStorageItems(e.DataView))
        {
            await viewModel.FileSystemHelpers.PerformOperationTypeAsync(viewModel, e.AcceptedOperation, e.DataView, viewModel.FileSystemViewModel.WorkingDirectory, false, true);
            await viewModel.RefreshIfNoWatcherExistsAsync();
        }
    }

    public void Dispose()
    {
        
    }
}