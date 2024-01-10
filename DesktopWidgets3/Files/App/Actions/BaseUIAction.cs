// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.ViewModels.Pages.Widget;

namespace Files.App.Actions;

/// <summary>
/// Represents base class for the UI Actions.
/// </summary>
internal abstract class BaseUIAction : ObservableObject
{
    protected readonly FolderViewViewModel context;

    public virtual bool IsExecutable => context.CanShowDialog;

    public BaseUIAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        context.PropertyChanged += UIHelpers_PropertyChanged;
    }

    private void UIHelpers_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(context.CanShowDialog))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}
