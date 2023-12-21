// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

/*using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;
using Files.App.Helpers;
using System.ComponentModel;

namespace Files.App.Actions;

internal class CutItemAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel _viewModel;

    public string Label
        => "Cut".GetLocalizedResource();

    public string Description
        => "CutItemDescription".GetLocalizedResource();

    public RichGlyph Glyph
        => new(opacityStyle: "ColorIconCut");

    *//*public HotKey HotKey
        => new(Keys.X, KeyModifiers.Ctrl);*//*

    public bool IsExecutable 
        => _viewModel.HasSelection;

    public CutItemAction(FolderViewViewModel viewModel)
    {
        _viewModel = viewModel;

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public Task ExecuteAsync()
    {
        return UIFilesystemHelpers.CutItemAsync(context.ShellPage);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(_viewModel.HasSelection))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}*/
