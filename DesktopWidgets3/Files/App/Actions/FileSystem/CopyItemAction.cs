// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;
using Files.App.Helpers;
using System.ComponentModel;

namespace Files.App.Actions;

internal class CopyItemAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    public string Label
        => "Copy".GetLocalized();

    public string Description
        => "CopyItemDescription".GetLocalized();

    public RichGlyph Glyph
        => new(opacityStyle: "ColorIconCopy");

    /*public HotKey HotKey
        => new(Keys.C, KeyModifiers.Ctrl);*/

    public bool IsExecutable
        => context.HasSelection;

    public CopyItemAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        context.PropertyChanged += Context_PropertyChanged;
    }

    public Task ExecuteAsync()
    {
        if (context is not null)
        {
            return UIFileSystemHelpers.CopyItemAsync(context);
        }

        return Task.CompletedTask;
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(context.HasSelection))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
