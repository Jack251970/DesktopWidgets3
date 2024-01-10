// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;

namespace Files.App.Actions;

internal class RefreshItemsAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    public string Label
        => "Refresh".GetLocalized();

    public string Description
        => "RefreshItemsDescription".GetLocalized();

    public RichGlyph Glyph
        => new("\uE72C");

    /*public HotKey HotKey
        => new(Keys.R, KeyModifiers.Ctrl);

    public HotKey SecondHotKey
        => new(Keys.F5);*/

    public bool IsExecutable
        => context.CanRefresh;

    public RefreshItemsAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        context.PropertyChanged += Context_PropertyChanged;
    }

    public async Task ExecuteAsync()
    {
        if (context is null)
        {
            return;
        }

        await context.Refresh_Click();
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(context.CanRefresh):
                OnPropertyChanged(nameof(IsExecutable));
                break;
        }
    }
}
