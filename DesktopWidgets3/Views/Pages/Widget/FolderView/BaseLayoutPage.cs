// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App;
using Files.App.Data.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;

namespace DesktopWidgets3.Views.Pages.Widget;

/// <summary>
/// Represents the base class which every layout page must derive from
/// </summary>
public class BaseLayoutPage : Page // , INotifyPropertyChanged
{
    // ViewModels
    public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }
    public ItemManipulationModel ItemManipulationModel { get; private set; }

    // Fields
    private CancellationTokenSource? shellContextMenuItemCancellationToken;

    private bool shiftPressed;

    // Properties

    public static CommandBarFlyout? LastOpenedFlyout { get; set; }

    public CommandBarFlyout ItemContextMenuFlyout { get; set; } = new()
    {
        AlwaysExpanded = true,
        AreOpenCloseAnimationsEnabled = false,
        Placement = FlyoutPlacementMode.Right,
    };

    private bool isItemSelected = false;
    public bool IsItemSelected
    {
        get => isItemSelected;
        internal set
        {
            if (value != isItemSelected)
            {
                isItemSelected = value;

                // NotifyPropertyChanged(nameof(IsItemSelected));
            }
        }
    }

    public async void ItemContextFlyout_Opening(object? sender, object e)
    {
        LastOpenedFlyout = sender as CommandBarFlyout;

        try
        {
            // Workaround for item sometimes not getting selected
            if (!IsItemSelected && sender is CommandBarFlyout { Target: ListViewItem { Content: ListedItem li } })
            {
                ItemManipulationModel.SetSelectedItem(li);
            }

            if (IsItemSelected)
            {
                // Reset menu max height
                if (ItemContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
                {
                    itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;
                }

                shellContextMenuItemCancellationToken?.Cancel();
                shellContextMenuItemCancellationToken = new CancellationTokenSource();
                SelectedItemsPropertiesViewModel.CheckAllFileExtensions(SelectedItems!.Select(selectedItem => selectedItem?.FileExtension).ToList()!);

                shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
                var items = ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: InstanceViewModel!, selectedItems: SelectedItems!, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: CommandsViewModel!, shiftPressed: shiftPressed, itemViewModel: null);

                ItemContextMenuFlyout.PrimaryCommands.Clear();
                ItemContextMenuFlyout.SecondaryCommands.Clear();

                var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
                AddCloseHandler(ItemContextMenuFlyout, primaryElements, secondaryElements);
                primaryElements.ForEach(ItemContextMenuFlyout.PrimaryCommands.Add);
                secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
                secondaryElements.ForEach(ItemContextMenuFlyout.SecondaryCommands.Add);

                if (InstanceViewModel!.CanTagFilesInPage)
                {
                    AddNewFileTagsToMenu(ItemContextMenuFlyout);
                }

                if (!InstanceViewModel.IsPageTypeZipFolder && !InstanceViewModel.IsPageTypeFtp)
                {
                    var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(workingDir: ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, selectedItems: SelectedItems!, shiftPressed: shiftPressed, showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
                    if (shellMenuItems.Any())
                    {
                        await AddShellMenuItemsAsync(shellMenuItems, ItemContextMenuFlyout, shiftPressed);
                    }
                    else
                    {
                        RemoveOverflow(ItemContextMenuFlyout);
                    }
                }
                else
                {
                    RemoveOverflow(ItemContextMenuFlyout);
                }
            }
        }
        catch (Exception)
        {

        }
    }

    public class ContextMenuExtensions : DependencyObject
    {
        public static ItemsControl GetItemsControl(DependencyObject obj)
        {
            return (ItemsControl)obj.GetValue(ItemsControlProperty);
        }

        public static void SetItemsControl(DependencyObject obj, ItemsControl value)
        {
            obj.SetValue(ItemsControlProperty, value);
        }

        public static readonly DependencyProperty ItemsControlProperty =
            DependencyProperty.RegisterAttached("ItemsControl", typeof(ItemsControl), typeof(ContextMenuExtensions), new PropertyMetadata(null));
    }
}