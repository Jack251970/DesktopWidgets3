﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using DesktopWidgets3.Files.Shared.Extensions;
using DesktopWidgets3.Files.Core.Data.Messages;
using DesktopWidgets3.Files.Core.Data.Enums;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Files.App.Extensions;
using DesktopWidgets3.Files.Core.Services;

namespace DesktopWidgets3.Files.Core.ViewModels.Dialogs.FileSystemDialog;

public sealed class FileSystemDialogViewModel : BaseDialogViewModel, IRecipient<FileSystemDialogOptionChangedMessage>
{
    private readonly FolderViewViewModel FolderViewViewModel;

    private readonly CancellationTokenSource _dialogClosingCts;

    private readonly IMessenger _messenger;

    public ObservableCollection<BaseFileSystemDialogItemViewModel> Items
    {
        get;
    }

    public FileSystemDialogMode FileSystemDialogMode
    {
        get;
    }

    private FileNameConflictResolveOptionType _AggregatedResolveOption;
    public FileNameConflictResolveOptionType AggregatedResolveOption
    {
        get => _AggregatedResolveOption;
        set
        {
            if (SetProperty(ref _AggregatedResolveOption, value))
            {
                ApplyConflictOptionToAll(value);
            }
        }
    }

    private string? _Description;
    public string? Description
    {
        get => _Description;
        set => SetProperty(ref _Description, value);
    }

    private bool _DeletePermanently;
    public bool DeletePermanently
    {
        get => _DeletePermanently;
        set => SetProperty(ref _DeletePermanently, value);
    }

    private bool _IsDeletePermanentlyEnabled;
    public bool IsDeletePermanentlyEnabled
    {
        get => _IsDeletePermanentlyEnabled;
        set => SetProperty(ref _IsDeletePermanentlyEnabled, value);
    }

    private FileSystemDialogViewModel(FolderViewViewModel viewModel, FileSystemDialogMode fileSystemDialogMode, IEnumerable<BaseFileSystemDialogItemViewModel> items)
    {
        FolderViewViewModel = viewModel;

        FileSystemDialogMode = fileSystemDialogMode;

        _dialogClosingCts = new();

        _messenger = new WeakReferenceMessenger();
        _messenger.Register(this);

        foreach (var item in items)
        {
            item.Messenger = _messenger;
        }

        Items = new(items);

        SecondaryButtonClickCommand = new RelayCommand(SecondaryButtonClick);
    }

    public bool IsNameAvailableForItem(BaseFileSystemDialogItemViewModel item, string name)
    {
        return Items.Where(x => !x.SourcePath!.Equals(item.SourcePath)).Cast<FileSystemDialogConflictItemViewModel>().All(x => x.DestinationDisplayName != name);
    }

    public void ApplyConflictOptionToAll(FileNameConflictResolveOptionType e)
    {
        if (!FileSystemDialogMode.IsInDeleteMode &&
            e != FileNameConflictResolveOptionType.None)
        {
            foreach (var item in Items)
            {
                if (item is FileSystemDialogConflictItemViewModel conflictItem && conflictItem.ConflictResolveOption != FileNameConflictResolveOptionType.None)
                {
                    conflictItem.ConflictResolveOption = e;
                }
            }

            PrimaryButtonEnabled = true;
        }
    }

    public IEnumerable<IFileSystemDialogConflictItemViewModel> GetItemsResult()
    {
        return Items.Cast<IFileSystemDialogConflictItemViewModel>();
    }

    public void Receive(FileSystemDialogOptionChangedMessage message)
    {
        if (message.Value.ConflictResolveOption != FileNameConflictResolveOptionType.None)
        {
            var itemsWithoutNone = Items.Where(x => (x as FileSystemDialogConflictItemViewModel)!.ConflictResolveOption != FileNameConflictResolveOptionType.None);

            // If all items have the same resolve option -- set the aggregated option to that choice
            var first = (itemsWithoutNone.First() as FileSystemDialogConflictItemViewModel)!.ConflictResolveOption;

            AggregatedResolveOption = itemsWithoutNone.All(x
                => (x as FileSystemDialogConflictItemViewModel)!.ConflictResolveOption == first)
                    ? first
                    : FileNameConflictResolveOptionType.None;
        }
    }

    public FileNameConflictResolveOptionType LoadConflictResolveOption()
    {
        return FolderViewViewModel.GetSettings().ConflictsResolveOption;
    }

    public void SaveConflictResolveOption()
    {
        // TODO: Save the option to the settings
        /*if (AggregatedResolveOption != FileNameConflictResolveOptionType.None &&
            AggregatedResolveOption != _viewModel.GetSettings().ConflictsResolveOption)
        {
            _viewModel.GetSettings().ConflictsResolveOption = AggregatedResolveOption;
        }*/
    }

    public void CancelCts()
    {
        _dialogClosingCts.Cancel();
    }

    private void SecondaryButtonClick()
    {
        ApplyConflictOptionToAll(FileNameConflictResolveOptionType.Skip);
    }

    public static FileSystemDialogViewModel GetDialogViewModel(FolderViewViewModel folderViewModel, FileSystemDialogMode dialogMode, (bool deletePermanently, bool IsDeletePermanentlyEnabled) deleteOption, FileSystemOperationType operationType, List<BaseFileSystemDialogItemViewModel> nonConflictingItems, List<BaseFileSystemDialogItemViewModel> conflictingItems)
    {
        var titleText = string.Empty;
        var descriptionText = string.Empty;
        var primaryButtonText = string.Empty;
        var secondaryButtonText = string.Empty;
        var isInDeleteMode = false;

        if (dialogMode.ConflictsExist)
        {
            // Subtitle text
            if (conflictingItems.Count > 1)
            {
                var descriptionLocalized = (nonConflictingItems.Count > 0)
                    ? "ConflictingItemsDialogSubtitleMultipleConflictsMultipleNonConflicts".GetLocalized()
                    : "ConflictingItemsDialogSubtitleMultipleConflictsNoNonConflicts".GetLocalized();

                descriptionText = string.Format(descriptionLocalized, conflictingItems.Count, nonConflictingItems.Count);
            }
            else
            {
                descriptionText = (nonConflictingItems.Count > 0)
                    ? string.Format("ConflictingItemsDialogSubtitleSingleConflictMultipleNonConflicts".GetLocalized(), nonConflictingItems.Count)
                    : string.Format("ConflictingItemsDialogSubtitleSingleConflictNoNonConflicts".GetLocalized(), conflictingItems.Count);
            }

            titleText = "ConflictingItemsDialogTitle".GetLocalized();
            primaryButtonText = "ConflictingItemsDialogPrimaryButtonText".GetLocalized();
            secondaryButtonText = "Cancel".GetLocalized();
        }
        else
        {
            switch (operationType)
            {
                case FileSystemOperationType.Copy:
                    {
                        titleText = "CopyItemsDialogTitle".GetLocalized();

                        descriptionText = (nonConflictingItems.Count + conflictingItems.Count == 1)
                            ? "CopyItemsDialogSubtitleSingle".GetLocalized()
                            : string.Format("CopyItemsDialogSubtitleMultiple".GetLocalized(), nonConflictingItems.Count + conflictingItems.Count);

                        primaryButtonText = "Copy".GetLocalized();
                        secondaryButtonText = "Cancel".GetLocalized();

                        break;
                    }

                case FileSystemOperationType.Move:
                    {
                        titleText = "MoveItemsDialogTitle".GetLocalized();

                        descriptionText = (nonConflictingItems.Count + conflictingItems.Count == 1)
                            ? "MoveItemsDialogSubtitleSingle".GetLocalized()
                            : string.Format("MoveItemsDialogSubtitleMultiple".GetLocalized(), nonConflictingItems.Count + conflictingItems.Count);

                        primaryButtonText = "MoveItemsDialogPrimaryButtonText".GetLocalized();
                        secondaryButtonText = "Cancel".GetLocalized();

                        break;
                    }

                case FileSystemOperationType.Delete:
                    {
                        titleText = "DeleteItemsDialogTitle".GetLocalized();

                        descriptionText = (nonConflictingItems.Count + conflictingItems.Count == 1)
                            ? "DeleteItemsDialogSubtitleSingle".GetLocalized()
                            : string.Format("DeleteItemsDialogSubtitleMultiple".GetLocalized(), nonConflictingItems.Count);

                        primaryButtonText = "Delete".GetLocalized();
                        secondaryButtonText = "Cancel".GetLocalized();

                        isInDeleteMode = true;

                        break;
                    }
            }
        }

        var viewModel = new FileSystemDialogViewModel(
            folderViewModel,
            new()
            {
                IsInDeleteMode = isInDeleteMode,
                ConflictsExist = !conflictingItems.IsEmpty()
            },
            conflictingItems.Concat(nonConflictingItems))
        {
            Title = titleText,
            Description = descriptionText,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            DeletePermanently = deleteOption.deletePermanently,
            IsDeletePermanentlyEnabled = deleteOption.IsDeletePermanentlyEnabled
        };

        _ = LoadItemsIcon(viewModel.Items, viewModel._dialogClosingCts.Token);

        return viewModel;
    }

    public static FileSystemDialogViewModel GetDialogViewModel(FolderViewViewModel folderViewModel, List<BaseFileSystemDialogItemViewModel> nonConflictingItems, string titleText, string descriptionText, string primaryButtonText, string secondaryButtonText)
    {
        var viewModel = new FileSystemDialogViewModel(
            folderViewModel,
            new()
            {
                IsInDeleteMode = false,
                ConflictsExist = false
            },
            nonConflictingItems)
        {
            Title = titleText,
            Description = descriptionText,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            DeletePermanently = false,
            IsDeletePermanentlyEnabled = false
        };

        _ = LoadItemsIcon(viewModel.Items, viewModel._dialogClosingCts.Token);
        return viewModel;
    }

    private static Task LoadItemsIcon(IEnumerable<BaseFileSystemDialogItemViewModel> items, CancellationToken token)
    {
        var imagingService = DesktopWidgets3.App.GetService<IImagingService>();

        var task = items.ParallelForEachAsync(async (item) =>
        {
            try
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await DesktopWidgets3.App.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
                {
                    item.ItemIcon = await imagingService.GetImageModelFromPathAsync(item.SourcePath!, 64u);
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        },
        10,
        token);

        return task;
    }
}

public sealed class FileSystemDialogMode
{
    /// <summary>
    /// Determines whether to show delete options for the dialog.
    /// </summary>
    public bool IsInDeleteMode
    {
        get; init;
    }

    /// <summary>
    /// Determines whether conflicts are visible.
    /// </summary>
    public bool ConflictsExist
    {
        get; init;
    }
}
