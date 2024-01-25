// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Files.App.Helpers;

public static class DynamicDialogFactory
{
	public static readonly SolidColorBrush _transparentBrush = new(Colors.Transparent);

	public static DynamicDialog GetFor_PropertySaveErrorDialog()
	{
		var dialog = new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = "PropertySaveErrorDialog/Title".ToLocalized(),
			SubtitleText = "PropertySaveErrorMessage/Text".ToLocalized(), // We can use subtitle here as our content
			PrimaryButtonText = "Retry".ToLocalized(),
			SecondaryButtonText = "PropertySaveErrorDialog/SecondaryButtonText".ToLocalized(),
			CloseButtonText = "Cancel".ToLocalized(),
			DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary | DynamicDialogButtons.Cancel
		});
		return dialog;
	}

	public static DynamicDialog GetFor_ConsentDialog()
	{
		var dialog = new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = "WelcomeDialog/Title".ToLocalized(),
			SubtitleText = "WelcomeDialogTextBlock/Text".ToLocalized(), // We can use subtitle here as our content
			PrimaryButtonText = "WelcomeDialog/PrimaryButtonText".ToLocalized(),
			PrimaryButtonAction = async (vm, e) => await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess")),
			DynamicButtons = DynamicDialogButtons.Primary
		});
		return dialog;
	}

	public static DynamicDialog GetFor_ShortcutNotFound(string targetPath)
	{
		DynamicDialog dialog = new(new DynamicDialogViewModel
		{
			TitleText = "ShortcutCannotBeOpened".ToLocalized(),
			SubtitleText = string.Format("DeleteShortcutDescription".ToLocalized(), targetPath),
			PrimaryButtonText = "Delete".ToLocalized(),
			SecondaryButtonText = "No".ToLocalized(),
			DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
		});
		return dialog;
	}

	public static DynamicDialog GetFor_RenameDialog()
	{
		DynamicDialog? dialog = null;
		TextBox inputText = new()
		{
			PlaceholderText = "EnterAnItemName".ToLocalized()
		};

		TeachingTip warning = new()
		{
			Title = "InvalidFilename/Text".ToLocalized(),
			PreferredPlacement = TeachingTipPlacementMode.Bottom,
			DataContext = new RenameDialogViewModel(),
		};

		warning.SetBinding(TeachingTip.TargetProperty, new Binding()
		{
			Source = inputText
		});
		warning.SetBinding(TeachingTip.IsOpenProperty, new Binding()
		{
			Mode = BindingMode.OneWay,
			Path = new PropertyPath("IsNameInvalid")
		});

		inputText.Resources.Add("InvalidNameWarningTip", warning);

		inputText.TextChanged += (textBox, args) =>
		{
            var isInputValid = false;/*FilesystemHelpers.IsValidForFilename(inputText.Text);*/ // TODO: Add support.
			((RenameDialogViewModel)warning.DataContext).IsNameInvalid = !string.IsNullOrEmpty(inputText.Text) && !isInputValid;
			dialog!.ViewModel.DynamicButtonsEnabled = isInputValid
													? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
													: DynamicDialogButtons.Cancel;
			if (isInputValid)
            {
                dialog.ViewModel.AdditionalData = inputText.Text;
            }
        };

		inputText.Loaded += (s, e) =>
		{
			// dispatching to the ui thread fixes an issue where the primary dialog button would steal focus
			_ = inputText.DispatcherQueue.EnqueueOrInvokeAsync(() => inputText.Focus(FocusState.Programmatic));
		};

		dialog = new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = "EnterAnItemName".ToLocalized(),
			SubtitleText = null!,
			DisplayControl = new Grid()
			{
				MinWidth = 300d,
				Children =
				{
					inputText
				}
			},
			PrimaryButtonAction = (vm, e) =>
			{
				vm.HideDialog(); // Rename successful
			},
			PrimaryButtonText = "RenameDialog/PrimaryButtonText".ToLocalized(),
			CloseButtonText = "Cancel".ToLocalized(),
			DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
			DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
		});

		dialog.Closing += (s, e) =>
		{
			warning.IsOpen = false;
		};

		return dialog;
	}

	public static DynamicDialog GetFor_FileInUseDialog(List<Win32Process> lockingProcess = null!)
	{
		var dialog = new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = "FileInUseDialog/Title".ToLocalized(),
			SubtitleText = lockingProcess.IsEmpty() ? "FileInUseDialog/Text".ToLocalized() :
				string.Format("FileInUseByDialog/Text".ToLocalized(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})"))),
			PrimaryButtonText = "OK".ToLocalized(),
			DynamicButtons = DynamicDialogButtons.Primary
		});
		return dialog;
	}

	public static DynamicDialog GetFor_CredentialEntryDialog(string path)
	{
		var userAndPass = new string[3];
		DynamicDialog? dialog = null;

		TextBox inputUsername = new()
		{
			PlaceholderText = "CredentialDialogUserName/PlaceholderText".ToLocalized()
		};

		PasswordBox inputPassword = new()
		{
			PlaceholderText = "Password".ToLocalized()
		};

		CheckBox saveCreds = new()
		{
			Content = "NetworkAuthenticationSaveCheckbox".ToLocalized()
		};

		inputUsername.TextChanged += (textBox, args) =>
		{
			userAndPass[0] = inputUsername.Text;
			dialog!.ViewModel.AdditionalData = userAndPass;
		};

		inputPassword.PasswordChanged += (textBox, args) =>
		{
			userAndPass[1] = inputPassword.Password;
			dialog!.ViewModel.AdditionalData = userAndPass;
		};

		saveCreds.Checked += (textBox, args) =>
		{
			userAndPass[2] = "y";
			dialog!.ViewModel.AdditionalData = userAndPass;
		};

		saveCreds.Unchecked += (textBox, args) =>
		{
			userAndPass[2] = "n";
			dialog!.ViewModel.AdditionalData = userAndPass;
		};

		dialog = new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = "NetworkAuthenticationDialogTitle".ToLocalized(),
			PrimaryButtonText = "OK".ToLocalized(),
			CloseButtonText = "Cancel".ToLocalized(),
			SubtitleText = string.Format("NetworkAuthenticationDialogMessage".ToLocalized(), path[2..]),
			DisplayControl = new Grid()
			{
				MinWidth = 250d,
				Children =
				{
					new StackPanel()
					{
						Spacing = 10d,
						Children =
						{
							inputUsername,
							inputPassword,
							saveCreds
						}
					}
				}
			},
			CloseButtonAction = (vm, e) =>
			{
				dialog!.ViewModel.AdditionalData = null!;
				vm.HideDialog();
			}

		});

		return dialog;
	}

	public static DynamicDialog GetFor_GitCheckoutConflicts(string checkoutBranchName, string headBranchName)
	{
		DynamicDialog dialog = null!;

        var optionsListView = new ListView
        {
            ItemsSource = new string[]
            {
                string.Format("BringChanges".ToLocalized(), checkoutBranchName),
                string.Format("StashChanges".ToLocalized(), headBranchName),
                "DiscardChanges".ToLocalized()
            },
            SelectionMode = ListViewSelectionMode.Single,
            SelectedIndex = 0
        };

        optionsListView.SelectionChanged += (listView, args) =>
		{
			dialog.ViewModel.AdditionalData = (GitCheckoutOptions)optionsListView.SelectedIndex;
		};

		dialog = new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = "SwitchBranch".ToLocalized(),
			PrimaryButtonText = "Switch".ToLocalized(),
			CloseButtonText = "Cancel".ToLocalized(),
			SubtitleText = "UncommittedChanges".ToLocalized(),
			DisplayControl = new Grid()
			{
				MinWidth = 250d,
				Children =
				{
					optionsListView
				}
			},
			AdditionalData = GitCheckoutOptions.BringChanges,
			CloseButtonAction = (vm, e) =>
			{
				dialog.ViewModel.AdditionalData = GitCheckoutOptions.None;
				vm.HideDialog();
			}
		});

		return dialog;
	}

	public static DynamicDialog GetFor_GitHubConnectionError()
	{
		var dialog = new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = "Error".ToLocalized(),
			SubtitleText = "CannotReachGitHubError".ToLocalized(),
			PrimaryButtonText = "Close".ToLocalized(),
			DynamicButtons = DynamicDialogButtons.Primary
		});
		return dialog;
	}

	public static DynamicDialog GetFor_GitCannotInitializeqRepositoryHere()
	{
		return new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = "Error".ToLocalized(),
			SubtitleText = "CannotInitializeGitRepo".ToLocalized(),
			PrimaryButtonText = "Close".ToLocalized(),
			DynamicButtons = DynamicDialogButtons.Primary
		});
	}

	public static DynamicDialog GetFor_DeleteGitBranchConfirmation(string branchName)
	{
		DynamicDialog dialog = null!;
		dialog = new DynamicDialog(new DynamicDialogViewModel()
		{
			TitleText = "GitDeleteBranch".ToLocalized(),
			SubtitleText = string.Format("GitDeleteBranchSubtitle".ToLocalized(), branchName),
			PrimaryButtonText = "OK".ToLocalized(),
			CloseButtonText = "Cancel".ToLocalized(),
			AdditionalData = true,
			CloseButtonAction = (vm, e) =>
			{
				dialog.ViewModel.AdditionalData = false;
				vm.HideDialog();
			}
		});

		return dialog;
	}
}
