// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;
using Files.App.Data.Contexts;
using Files.App.Data.Models;
using Files.App.Helpers;
using System.ComponentModel;

namespace Files.App.Actions;

internal class PasteItemAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    public string Label
        => "Paste".ToLocalized();

    public string Description
        => "PasteItemDescription".ToLocalized();

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
