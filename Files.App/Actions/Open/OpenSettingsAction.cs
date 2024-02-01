// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal class OpenSettingsAction : BaseUIAction, IAction
{
	private readonly IDialogService dialogService;

	private readonly SettingsDialogViewModel viewModel = new();

    public string Label
		=> "Settings".GetLocalizedResource();

	public string Description
		=> "OpenSettingsDescription".GetLocalizedResource();

	public HotKey HotKey
		=> new(Keys.OemComma, KeyModifiers.Ctrl);

    public OpenSettingsAction(IFolderViewViewModel folderViewViewModel) : base(folderViewViewModel)
    {
        dialogService = folderViewViewModel.GetService<IDialogService>();
    }

    public Task ExecuteAsync()
	{
		var dialog = dialogService.GetDialog(viewModel);
		return dialog.TryShowAsync(FolderViewViewModel);
	}
}
