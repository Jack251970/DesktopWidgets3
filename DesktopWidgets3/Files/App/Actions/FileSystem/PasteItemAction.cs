// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Files.App.Data.Commands;
using DesktopWidgets3.Files.App.Data.Contexts;
using DesktopWidgets3.Files.App.Data.Models;
using DesktopWidgets3.Files.App.Helpers;
using System.ComponentModel;

namespace DesktopWidgets3.Files.App.Actions;

internal class PasteItemAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    public string Label
        => "Paste".GetLocalized();

    public string Description
        => "PasteItemDescription".GetLocalized();

    public RichGlyph Glyph
        => new(opacityStyle: "ColorIconPaste");

    /*public HotKey HotKey
        => new(Keys.V, KeyModifiers.Ctrl);*/

    public bool IsExecutable
        => GetIsExecutable();

    public PasteItemAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        context.PropertyChanged += Context_PropertyChanged;
        DesktopWidgets3.App.AppModel.PropertyChanged += AppModel_PropertyChanged;
    }

    public async Task ExecuteAsync()
    {
        if (context is null)
        {
            return;
        }

        var path = context.FileSystemViewModel.WorkingDirectory;
        await UIFileSystemHelpers.PasteItemAsync(path, context);
    }

    public bool GetIsExecutable()
    {
        return
            DesktopWidgets3.App.AppModel.IsPasteEnabled &&
            context.PageType != ContentPageTypes.Home &&
            context.PageType != ContentPageTypes.RecycleBin &&
            context.PageType != ContentPageTypes.SearchResults;
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(context.PageType))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }

    private void AppModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppModel.IsPasteEnabled))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
