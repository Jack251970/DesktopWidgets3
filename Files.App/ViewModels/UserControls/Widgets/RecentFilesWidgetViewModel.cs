﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.IO;
using Windows.Foundation.Metadata;

namespace Files.App.ViewModels.UserControls.Widgets;

/// <summary>
/// Represents view model of <see cref="RecentFilesWidget"/>.
/// </summary>
public sealed class RecentFilesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
{
    // Fields

    private readonly SemaphoreSlim _refreshRecentFilesSemaphore;
    private CancellationTokenSource _refreshRecentFilesCTS;

    // Properties

    public ObservableCollection<RecentItem> Items { get; } = [];

    public string WidgetName => nameof(RecentFilesWidget);
    public string AutomationProperties => "RecentFiles".GetLocalizedResource();
    public string WidgetHeader => "RecentFiles".GetLocalizedResource();
    public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
    public bool ShowMenuFlyout => false;
    public MenuFlyoutItem? MenuFlyoutItem => null;

    private bool _IsEmptyRecentFilesTextVisible;
    public bool IsEmptyRecentFilesTextVisible
    {
        get => _IsEmptyRecentFilesTextVisible;
        set => SetProperty(ref _IsEmptyRecentFilesTextVisible, value);
    }

    private bool _IsRecentFilesDisabledInWindows;
    public bool IsRecentFilesDisabledInWindows
    {
        get => _IsRecentFilesDisabledInWindows;
        set => SetProperty(ref _IsRecentFilesDisabledInWindows, value);
    }

    // Constructor

    public RecentFilesWidgetViewModel()
    {
        _refreshRecentFilesSemaphore = new SemaphoreSlim(1, 1);
        _refreshRecentFilesCTS = new CancellationTokenSource();

        // recent files could have changed while widget wasn't loaded
        /*_ = RefreshWidgetAsync();*/

        /*App.RecentItemsManager.RecentFilesChanged += Manager_RecentFilesChanged;*/

        RemoveRecentItemCommand = new AsyncRelayCommand<RecentItem>(ExecuteRemoveRecentItemCommand);
        ClearAllItemsCommand = new AsyncRelayCommand(ExecuteClearRecentItemsCommand);
        OpenFileLocationCommand = new RelayCommand<RecentItem>(ExecuteOpenFileLocationCommand);
        OpenPropertiesCommand = new RelayCommand<RecentItem>(ExecuteOpenPropertiesCommand);
    }

    // CHANGE: Refresh widget after initialization.
    public async new void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        base.Initialize(folderViewViewModel);

        await RefreshWidgetAsync();

        if (App.RecentItemsManager.RecentFilesChangedManager.Get(folderViewViewModel) is EventHandler<NotifyCollectionChangedEventArgs> eventHandler)
        {
            eventHandler += Manager_RecentFilesChanged;
        }

        // CHANGE: Mannuly trigger reset event.
        Manager_RecentFilesChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    // Methods

    public async Task RefreshWidgetAsync()
    {
        IsRecentFilesDisabledInWindows = App.RecentItemsManager.CheckIsRecentFilesEnabled() is false;
        await App.RecentItemsManager.UpdateRecentFilesAsync(FolderViewViewModel);
    }

    public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
    {
        return new List<ContextMenuFlyoutItemViewModel>()
        {
            new()
            {
                Text = "OpenWith".GetLocalizedResource(),
                OpacityIcon = new() { OpacityIconStyle = "ColorIconOpenWith" },
                Tag = "OpenWithPlaceholder",
            },
            new()
            {
                Text = "RecentItemRemove/Text".GetLocalizedResource(),
                Glyph = "\uE738",
                Command = RemoveRecentItemCommand,
                CommandParameter = item
            },
            new()
            {
                Text = "RecentItemClearAll/Text".GetLocalizedResource(),
                Glyph = "\uE74D",
                Command = ClearAllItemsCommand
            },
            new()
            {
                Text = "OpenFileLocation".GetLocalizedResource(),
                Glyph = "\uED25",
                Command = OpenFileLocationCommand,
                CommandParameter = item
            },
            new()
            {
                Text = "SendTo".GetLocalizedResource(),
                Tag = "SendToPlaceholder",
                ShowItem = UserSettingsService.GeneralSettingsService.ShowSendToMenu
            },
            new()
            {
                Text = "Properties".GetLocalizedResource(),
                OpacityIcon = new() { OpacityIconStyle = "ColorIconProperties" },
                Command = OpenPropertiesCommand,
                CommandParameter = item
            },
            new()
            {
                ItemType = ContextMenuFlyoutItemType.Separator,
                Tag = "OverflowSeparator",
            },
            new()
            {
                Text = "Loading".GetLocalizedResource(),
                Glyph = "\xE712",
                Items = [],
                ID = "ItemOverflow",
                Tag = "ItemOverflow",
                IsEnabled = false,
            }
        }.Where(x => x.ShowItem).ToList();
    }

    private async Task UpdateRecentFilesListAsync(NotifyCollectionChangedEventArgs e)
    {
        try
        {
            await _refreshRecentFilesSemaphore.WaitAsync(_refreshRecentFilesCTS.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            // drop other waiting instances
            _refreshRecentFilesCTS.Cancel();
            _refreshRecentFilesCTS = new CancellationTokenSource();

            IsEmptyRecentFilesTextVisible = false;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems is not null)
                    {
                        var addedItem = e.NewItems.Cast<RecentItem>().Single();
                        AddItemToRecentList(addedItem, 0);
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems is not null)
                    {
                        var movedItem = e.OldItems.Cast<RecentItem>().Single();
                        Items.RemoveAt(e.OldStartingIndex);
                        AddItemToRecentList(movedItem, 0);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is not null)
                    {
                        var removedItem = e.OldItems.Cast<RecentItem>().Single();
                        Items.RemoveAt(e.OldStartingIndex);
                    }
                    break;

                // case NotifyCollectionChangedAction.Reset:
                default:
                    var recentFiles = App.RecentItemsManager.GetRecentFiles(FolderViewViewModel); // already sorted, add all in order
                    if (!recentFiles.SequenceEqual(Items))
                    {
                        Items.Clear();
                        foreach (var item in recentFiles)
                        {
                            AddItemToRecentList(item);
                        }
                    }
                    break;
            }

            // update chevron if there aren't any items
            if (Items.Count == 0 && !IsRecentFilesDisabledInWindows)
            {
                IsEmptyRecentFilesTextVisible = true;
            }
        }
        catch (Exception ex)
        {
            App.Logger.LogInformation(ex, "Could not populate recent files");
        }
        finally
        {
            _refreshRecentFilesSemaphore.Release();
        }
    }

    private bool AddItemToRecentList(RecentItem? recentItem, int index = -1)
    {
        if (recentItem is null)
        {
            return false;
        }

        if (!Items.Any(x => x.Equals(recentItem)))
        {
            Items.Insert(index < 0 ? Items.Count : Math.Min(index, Items.Count), recentItem);
            _ = recentItem.LoadRecentItemIconAsync()
                .ContinueWith(t => App.Logger.LogWarning(t.Exception, null), TaskContinuationOptions.OnlyOnFaulted);
            return true;
        }
        return false;
    }

    public void NavigateToPath(string path)
    {
        try
        {
            var directoryName = Path.GetDirectoryName(path);

            _ = Win32Helper.InvokeWin32ComponentAsync(path, ContentPageContext.ShellPage!, workingDirectory: directoryName ?? string.Empty);
        }
        catch (Exception) { }
    }

    // Event methods

    private async void Manager_RecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        await ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(async () =>
        {
            // e.Action can only be Reset right now; naively refresh everything for simplicity
            await UpdateRecentFilesListAsync(e);
        });
    }

    // Command methods

    private async Task ExecuteRemoveRecentItemCommand(RecentItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _refreshRecentFilesSemaphore.WaitAsync();

        try
        {
            await App.RecentItemsManager.UnpinFromRecentFiles(item);
        }
        finally
        {
            _refreshRecentFilesSemaphore.Release();
        }
    }

    private async Task ExecuteClearRecentItemsCommand()
    {
        await _refreshRecentFilesSemaphore.WaitAsync();
        try
        {
            Items.Clear();
            var success = App.RecentItemsManager.ClearRecentItems();

            if (success)
            {
                IsEmptyRecentFilesTextVisible = true;
            }
        }
        finally
        {
            _refreshRecentFilesSemaphore.Release();
        }
    }

    private void ExecuteOpenFileLocationCommand(RecentItem? item)
    {
        if (item is null)
        {
            return;
        }

        var itemPath = Directory.GetParent(item.RecentPath)?.FullName ?? string.Empty;
        var itemName = Path.GetFileName(item.RecentPath);

        ContentPageContext.ShellPage!.NavigateWithArguments(
            ContentPageContext.ShellPage!.InstanceViewModel.FolderSettings.GetLayoutType(itemPath),
            new NavigationArguments()
            {
                FolderViewViewModel = FolderViewViewModel,
                NavPathParam = itemPath,
                SelectItems = [itemName],
                AssociatedTabInstance = ContentPageContext.ShellPage!
            });
    }

    private void ExecuteOpenPropertiesCommand(RecentItem? item)
    {
        var flyout = HomePageContext.ItemContextFlyoutMenu;

        if (item is null || flyout is null)
        {
            return;
        }

        EventHandler<object> flyoutClosed = null!;
        flyoutClosed = async (s, e) =>
        {
            flyout!.Closed -= flyoutClosed;

            BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.Path));
            if (file is null)
            {
                ContentDialog dialog = new()
                {
                    Title = "CannotAccessPropertiesTitle".GetLocalizedResource(),
                    Content = "CannotAccessPropertiesContent".GetLocalizedResource(),
                    PrimaryButtonText = "Ok".GetLocalizedResource()
                };

                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    dialog.XamlRoot = FolderViewViewModel.XamlRoot;
                }

                await dialog.TryShowAsync(FolderViewViewModel);
            }
            else
            {
                var listedItem = await UniversalStorageEnumerator.AddFileAsync(FolderViewViewModel, file, null!, default);
                FilePropertiesHelpers.OpenPropertiesWindow(FolderViewViewModel, listedItem, ContentPageContext.ShellPage!);
            }
        };

        flyout!.Closed += flyoutClosed;
    }

    // Disposer

    public void Dispose()
    {
        // CHANGE: Remove event handler dispostion, which will be handled in the unregister method.
        /*if (App.RecentItemsManager.RecentFilesChangedManager.Get(FolderViewViewModel) is EventHandler<NotifyCollectionChangedEventArgs> eventHandler)
        {
            eventHandler -= Manager_RecentFilesChanged;
        }*/
    }
}
