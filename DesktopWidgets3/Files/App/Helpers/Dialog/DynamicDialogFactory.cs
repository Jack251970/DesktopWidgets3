// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml.Controls;
using Files.App.Extensions;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Files.App.Utils.Storage;

namespace Files.App.Helpers;

public static class DynamicDialogFactory
{
    public static DynamicDialog GetFor_FileInUseDialog(List<Win32Process> lockingProcess = null!)
    {
        var dialog = new DynamicDialog(new DynamicDialogViewModel()
        {
            TitleText = "FileInUseDialog/Title".GetLocalized(),
            SubtitleText = lockingProcess.IsEmpty() ? "FileInUseDialog/Text".GetLocalized() :
                string.Format("FileInUseByDialog/Text".GetLocalized(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})"))),
            PrimaryButtonText = "Ok".GetLocalized(),
            DynamicButtons = DynamicDialogButtons.Primary
        });
        return dialog;
    }

    public static DynamicDialog GetFor_ShortcutNotFound(string targetPath)
    {
        DynamicDialog dialog = new(new DynamicDialogViewModel
        {
            TitleText = "ShortcutCannotBeOpened".GetLocalized(),
            SubtitleText = string.Format("DeleteShortcutDescription".GetLocalized(), targetPath),
            PrimaryButtonText = "Delete".GetLocalized(),
            SecondaryButtonText = "No".GetLocalized(),
            DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
        });
        return dialog;
    }

    public static DynamicDialog GetFor_CredentialEntryDialog(string path)
    {
        var userAndPass = new string[3];
        DynamicDialog? dialog = null;

        TextBox inputUsername = new()
        {
            PlaceholderText = "CredentialDialogUserName/PlaceholderText".GetLocalized()
        };

        PasswordBox inputPassword = new()
        {
            PlaceholderText = "Password".GetLocalized()
        };

        CheckBox saveCreds = new()
        {
            Content = "NetworkAuthenticationSaveCheckbox".GetLocalized()
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
            TitleText = "NetworkAuthenticationDialogTitle".GetLocalized(),
            PrimaryButtonText = "OK".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized(),
            SubtitleText = string.Format("NetworkAuthenticationDialogMessage".GetLocalized(), path.Substring(2)),
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

    public static DynamicDialog GetFor_RenameDialog()
    {
        DynamicDialog? dialog = null;
        TextBox inputText = new()
        {
            PlaceholderText = "EnterAnItemName".GetLocalized()
        };

        TeachingTip warning = new()
        {
            Title = "InvalidFilename/Text".GetLocalized(),
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
            var isInputValid = FileSystemHelpers.IsValidForFilename(inputText.Text);
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
            TitleText = "EnterAnItemName".GetLocalized(),
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
            PrimaryButtonText = "RenameDialog/PrimaryButtonText".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized(),
            DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
            DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
        });

        dialog.Closing += (s, e) =>
        {
            warning.IsOpen = false;
        };

        return dialog;
    }
}
