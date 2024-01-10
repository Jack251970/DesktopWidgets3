// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;
using Files.App.Data.Contexts;

namespace Files.App.Actions;

internal class RenameAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    public string Label
        => "Rename".GetLocalized();

    public string Description
        => "RenameDescription".GetLocalized();

    /*public HotKey HotKey
        => new(Keys.F2);*/

    public RichGlyph Glyph
        => new(opacityStyle: "ColorIconRename");

    public bool IsExecutable =>
        context is not null &&
        IsPageTypeValid() &&
        /*context.ShellPage.SlimContentPage is not null &&*/
        IsSelectionValid();

    public RenameAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        context.PropertyChanged += Context_PropertyChanged;
    }

    public Task ExecuteAsync()
    {
        context.ItemManipulationModel.StartRenameItem();

        return Task.CompletedTask;
    }

    private bool IsSelectionValid()
    {
        return context.HasSelection && context.SelectedItems.Count == 1;
    }

    private bool IsPageTypeValid()
    {
        return
            context.PageType != ContentPageTypes.None &&
            context.PageType != ContentPageTypes.Home &&
            context.PageType != ContentPageTypes.RecycleBin &&
            context.PageType != ContentPageTypes.ZipFolder;
    }

    private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            /*case nameof(IContentPageContext.ShellPage):*/
            case nameof(context.PageType):
            case nameof(context.HasSelection):
            case nameof(context.SelectedItems):
                OnPropertyChanged(nameof(IsExecutable));
                break;
        }
    }
}
