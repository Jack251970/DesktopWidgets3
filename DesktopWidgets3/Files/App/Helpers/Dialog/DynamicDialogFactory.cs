// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Shared.Extensions;

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
}
