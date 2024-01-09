// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Files.App.Data.Commands;
using System.ComponentModel;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Helpers;

namespace DesktopWidgets3.Files.App.Actions;

internal class NavigateBackAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

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
        => context.CanGoBack && context.AllowNavigation;

    public NavigateBackAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        viewModel.PropertyChanged += Context_PropertyChanged;
    }

    public Task ExecuteAsync()
    {
        context.Back_Click();

        return Task.CompletedTask;
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(context.CanGoBack) ||
            e.PropertyName is nameof(context.AllowNavigation))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}