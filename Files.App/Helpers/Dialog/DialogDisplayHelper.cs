// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;

namespace Files.App.Helpers;

// TODO: change to internal.
public class DialogDisplayHelper
{
	/// <summary>
	/// Standard dialog, to ensure consistency.
	/// The secondaryText can be un-assigned to hide its respective button.
	/// Result is true if the user presses primary text button
	/// </summary>
	/// <param name="title">
	/// The title of this dialog
	/// </param>
	/// <param name="message">
	/// THe main body message displayed within the dialog
	/// </param>
	/// <param name="primaryText">
	/// Text to be displayed on the primary button (which returns true when pressed).
	/// If not set, defaults to 'OK'
	/// </param>
	/// <param name="secondaryText">
	/// The (optional) secondary button text.
	/// If not set, it won't be presented to the user at all.
	/// </param>
	public static async Task<bool> ShowDialogAsync(IFolderViewViewModel viewModel, string title, string message, string primaryText = null!, string secondaryText = null!)
	{
        if (string.IsNullOrEmpty(primaryText))
        {
            primaryText = "OK".ToLocalized();
        }

        var dialog = new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = title,
			SubtitleText = message, // We can use subtitle here as our actual message and skip DisplayControl
			PrimaryButtonText = primaryText,
			SecondaryButtonText = secondaryText,
			DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
		});

		return await ShowDialogAsync(viewModel, dialog) == DynamicDialogResult.Primary;
    }

	public static async Task<DynamicDialogResult> ShowDialogAsync(IFolderViewViewModel viewModel, DynamicDialog dialog)
	{
		try
		{
            await dialog.ShowAsync(viewModel);
            return dialog.DynamicResult;
        }
		catch (Exception)
		{
		}

		return DynamicDialogResult.Cancel;
	}
}