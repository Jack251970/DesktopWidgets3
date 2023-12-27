// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;

namespace Files.App.Actions;

internal class CutItemAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    public string Label
        => "Cut".GetLocalized();

    public string Description
        => "CutItemDescription".GetLocalized();

    public RichGlyph Glyph
        => new(opacityStyle: "ColorIconCut");

    /*public HotKey HotKey
        => new(Keys.X, KeyModifiers.Ctrl);*/

    public bool IsExecutable
        => context.HasSelection;

    public CutItemAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        context.PropertyChanged += Context_PropertyChanged;
    }

    public Task ExecuteAsync()
    {
        return /*context is not null
            ? UIFilesystemHelpers.CutItemAsync(context)
            : */Task.CompletedTask;
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(context.HasSelection))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
