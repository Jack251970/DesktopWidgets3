// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;
using System.ComponentModel;

namespace Files.App.Actions;

internal class NavigateUpAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel _viewModel;

    public string Label
        => "Up".GetLocalized();

    public string Description
        => "NavigateUpDescription".GetLocalized();

    public RichGlyph Glyph
        => new("\uE74A");

    /*public HotKey HotKey
        => new(Keys.Up, KeyModifiers.Menu);*/

    public bool IsExecutable
        => _viewModel.CanNavigateToParent && _viewModel.AllowNavigation;

    public NavigateUpAction(FolderViewViewModel viewModel)
    {
        _viewModel = viewModel;

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public Task ExecuteAsync()
    {
        _viewModel.Up_Click();

        return Task.CompletedTask;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(_viewModel.CanNavigateToParent) ||
            e.PropertyName is nameof(_viewModel.AllowNavigation))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}