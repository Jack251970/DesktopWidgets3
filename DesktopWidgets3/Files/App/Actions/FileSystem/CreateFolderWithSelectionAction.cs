// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Data.Commands;
using Files.App.Helpers;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;

namespace Files.App.Actions;

internal class CreateFolderWithSelectionAction : ObservableObject, IAction
{
    private readonly FolderViewViewModel context;

    public string Label
		=> "CreateFolderWithSelection".ToLocalized();

	public string Description
		=> "CreateFolderWithSelectionDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconNewFolder");

	public bool IsExecutable =>
		context is not null &&
		context.HasSelection;

	public CreateFolderWithSelectionAction(FolderViewViewModel viewModel)
    {
        context = viewModel;

        viewModel.PropertyChanged += Context_PropertyChanged;
    }

    public Task ExecuteAsync()
	{
		return UIFileSystemHelpers.CreateFolderWithSelectionAsync(context!);
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			/*case nameof(context.ShellPage):*/
			case nameof(context.HasSelection):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
