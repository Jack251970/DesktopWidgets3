// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;
using Files.App.Data.Contexts;
using Files.App.Helpers;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions;

internal class ShareItemAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    public string Label
        => "Share".ToLocalized();

    public string Description
        => "ShareItemDescription".ToLocalized();

    public RichGlyph Glyph
        => new(opacityStyle: "ColorIconShare");

    public bool IsExecutable =>
        IsContextPageTypeAdaptedToCommand() &&
        DataTransferManager.IsSupported() &&
        context.SelectedItems.Any() &&
        context.SelectedItems.All(ShareItemHelpers.IsItemShareable);

    public ShareItemAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        context.PropertyChanged += Context_PropertyChanged;
    }

    public Task ExecuteAsync()
    {
        ShareItemHelpers.ShareItems(context, context.SelectedItems);

        return Task.CompletedTask;
    }

    private bool IsContextPageTypeAdaptedToCommand()
    {
        return
            context.PageType != ContentPageTypes.RecycleBin &&
            context.PageType != ContentPageTypes.Home &&
            context.PageType != ContentPageTypes.Ftp &&
            context.PageType != ContentPageTypes.ZipFolder &&
            context.PageType != ContentPageTypes.None;
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(context.SelectedItems):
            case nameof(context.PageType):
                OnPropertyChanged(nameof(IsExecutable));
                break;
        }
    }
}
