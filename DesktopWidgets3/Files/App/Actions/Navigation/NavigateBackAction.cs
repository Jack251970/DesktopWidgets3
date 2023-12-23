﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Data.Commands;
using System.ComponentModel;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Helpers;

namespace Files.App.Actions;

internal class NavigateBackAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel _viewModel;

    public string Label
        => "Back".GetLocalized();

    public string Description
        => "NavigateBackDescription".GetLocalized();

    /*public HotKey HotKey
        => new(Keys.Left, KeyModifiers.Menu);

    public HotKey SecondHotKey
        => new(Keys.Back);

    public HotKey ThirdHotKey
        => new(Keys.Mouse4);

    public HotKey MediaHotKey
        => new(Keys.GoBack, false);*/

    public RichGlyph Glyph
        => new("\uE72B");

    public bool IsExecutable
        => _viewModel.IsNavigateBackExecutable;

    public NavigateBackAction(FolderViewViewModel viewModel)
    {
        _viewModel = viewModel;

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public Task ExecuteAsync()
    {
        _viewModel.NavigateBack();

        return Task.CompletedTask;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(_viewModel.IsNavigateBackExecutable))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}