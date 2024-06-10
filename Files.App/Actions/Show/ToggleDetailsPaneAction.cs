﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class ToggleDetailsPaneAction : ObservableObject, IToggleAction
{
	private readonly InfoPaneViewModel viewModel;
	private readonly IInfoPaneSettingsService infoPaneSettingsService;

	public string Label
		=> "ToggleDetailsPane".GetLocalizedResource();

	public string Description
		=> "ToggleDetailsPaneDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconRightPane");

	public HotKey HotKey
		=> new(Keys.D, KeyModifiers.CtrlAlt);

	public bool IsOn
		=> viewModel.IsEnabled;

	public ToggleDetailsPaneAction(IFolderViewViewModel folderViewViewModel)
	{
		viewModel = folderViewViewModel.GetRequiredService<InfoPaneViewModel>();
        infoPaneSettingsService = folderViewViewModel.GetRequiredService<IInfoPaneSettingsService>();

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
		viewModel.IsEnabled = true;
		infoPaneSettingsService.SelectedTab = InfoPaneTabs.Details;

		return Task.CompletedTask;
	}

	private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
			OnPropertyChanged(nameof(IsOn));
	}
}
