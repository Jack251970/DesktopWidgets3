// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;

namespace Files.App.Actions;

internal class DeleteItemAction : BaseDeleteAction, IAction
{
    public string Label
        => "Delete".ToLocalized();

    public string Description
        => "DeleteItemDescription".ToLocalized();

    public RichGlyph Glyph
        => new(opacityStyle: "ColorIconDelete");

    /*public HotKey HotKey
        => new(Keys.Delete);

    public HotKey SecondHotKey
        => new(Keys.D, KeyModifiers.Ctrl);*/

    public DeleteItemAction(FolderViewViewModel viewModel) : base(viewModel)
    {

    }

    public Task ExecuteAsync()
    {
        return DeleteItemsAsync(false);
    }
}