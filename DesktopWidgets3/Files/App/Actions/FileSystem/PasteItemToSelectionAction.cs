// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Commands;
using Files.App.Utils;
using Files.App.Data.Contexts;
using System.ComponentModel;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Helpers;
using Files.App.Helpers;

namespace Files.App.Actions;

internal class PasteItemToSelectionAction : BaseUIAction, IAction
{
    public string Label
        => "Paste".ToLocalized();

    public string Description
        => "PasteItemToSelectionDescription".ToLocalized();

    public RichGlyph Glyph
        => new(opacityStyle: "ColorIconPaste");

    /*public HotKey HotKey
        => new(Keys.V, KeyModifiers.CtrlShift);*/

    public override bool IsExecutable
        => GetIsExecutable();

    public PasteItemToSelectionAction(FolderViewViewModel viewModel) : base(viewModel)
    {
        context.PropertyChanged += Context_PropertyChanged;
        DesktopWidgets3.App.AppModel.PropertyChanged += AppModel_PropertyChanged;
    }

    public async Task ExecuteAsync()
    {
        if (context is null)
        {
            return;
        }

        var path = context.SelectedItem is ListedItem selectedItem
            ? selectedItem.ItemPath
            : context.FileSystemViewModel.WorkingDirectory;

        await UIFileSystemHelpers.PasteItemAsync(path, context);
    }

    public bool GetIsExecutable()
    {
        if (!DesktopWidgets3.App.AppModel.IsPasteEnabled)
        {
            return false;
        }

        if (context.PageType is ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.SearchResults)
        {
            return false;
        }

        if (!context.HasSelection)
        {
            return true;
        }

        return
            context.SelectedItem?.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder &&
            context.CanShowDialog;
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(context.PageType):
            case nameof(context.SelectedItem):
                OnPropertyChanged(nameof(IsExecutable));
                break;
        }
    }
    private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DesktopWidgets3.App.AppModel.IsPasteEnabled))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
