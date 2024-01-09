// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Files.App.Data.Commands;
using System.ComponentModel;

namespace DesktopWidgets3.Files.App.Actions;

internal class NavigateUpAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    public string Label
        => "Up".GetLocalized();

    public string Description
        => "NavigateUpDescription".GetLocalized();

    public RichGlyph Glyph
        => new("\uE74A");

    /*public HotKey HotKey
        => new(Keys.Up, KeyModifiers.Menu);*/

    public bool IsExecutable
        => context.CanNavigateToParent && context.AllowNavigation;

    public NavigateUpAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        viewModel.PropertyChanged += Context_PropertyChanged;
    }

    public Task ExecuteAsync()
    {
        context.Up_Click();

        return Task.CompletedTask;
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(context.CanNavigateToParent) ||
            e.PropertyName is nameof(context.AllowNavigation))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}