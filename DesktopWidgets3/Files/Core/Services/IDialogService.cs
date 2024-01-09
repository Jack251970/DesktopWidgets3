// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.Core.ViewModels.Dialogs;
using DesktopWidgets3.Files.Core.Data.Enums;
using System.ComponentModel;
using DesktopWidgets3.ViewModels.Pages.Widget;

namespace DesktopWidgets3.Files.Core.Services;

/// <summary>
/// A service to manage dialogs.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Initialize dialog service with associated <paramref name="viewModel"/>.
    /// </summary>
    /// <param name="folderViewModel">The view model of the dialogs.</param>
    void Initialize(FolderViewViewModel folderViewModel);

    /// <summary>
    /// Gets appropriate dialog with associated <paramref name="viewModel"/>.
    /// </summary>
    /// <typeparam name="TViewModel">The type of view model.</typeparam>
    /// <param name="viewModel">The view model of the dialog.</param>
    /// <returns>A new instance of <see cref="IDialog{TViewModel}"/> with associated <paramref name="viewModel"/>.</returns>
    IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel) where TViewModel : class, INotifyPropertyChanged;

    /// <summary>
    /// Creates and shows appropriate dialog derived from associated <paramref name="viewModel"/>.
    /// </summary>
    /// <typeparam name="TViewModel">The type of view model.</typeparam>
    /// <param name="viewModel">The view model of the dialog.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. Returns <see cref="DialogResult"/> based on the selected option.</returns>
    Task<DialogResult> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class, INotifyPropertyChanged;
}
