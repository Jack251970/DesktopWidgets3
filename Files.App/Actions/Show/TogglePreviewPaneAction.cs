﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class TogglePreviewPaneAction : ObservableObject, IToggleAction
{
	private readonly InfoPaneViewModel viewModel;
	private readonly IInfoPaneSettingsService infoPaneSettingsService;

	public string Label
		=> "TogglePreviewPane".GetLocalizedResource();

	public string Description
		=> "TogglePreviewPaneDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconRightPane");

	public HotKey HotKey
		=> new(Keys.P, KeyModifiers.CtrlAlt);

	public bool IsOn
		=> viewModel.IsEnabled;

	public TogglePreviewPaneAction(IFolderViewViewModel folderViewViewModel)
    {
        viewModel = folderViewViewModel.GetService<InfoPaneViewModel>();
        infoPaneSettingsService = folderViewViewModel.GetService<IInfoPaneSettingsService>();

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
	}

	public Task ExecuteAsync(object? parameter = null)
	{
		viewModel.IsEnabled = true;
		infoPaneSettingsService.SelectedTab = InfoPaneTabs.Preview;

		return Task.CompletedTask;
	}

	private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
        {
            OnPropertyChanged(nameof(IsOn));
        }
    }
}
