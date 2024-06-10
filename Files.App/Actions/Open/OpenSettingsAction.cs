﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class OpenSettingsAction(IFolderViewViewModel folderViewViewModel) : BaseUIAction(folderViewViewModel), IAction
{
	private readonly IDialogService dialogService = folderViewViewModel.GetRequiredService<IDialogService>();

	private readonly SettingsDialogViewModel viewModel = new();

    public string Label
		=> "Settings".GetLocalizedResource();

	public string Description
		=> "OpenSettingsDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.OemComma, KeyModifiers.Ctrl);

    public Task ExecuteAsync(object? parameter = null)
	{
		var dialog = dialogService.GetDialog(viewModel);
		return dialog.TryShowAsync(FolderViewViewModel);
	}
}
