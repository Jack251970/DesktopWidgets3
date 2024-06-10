// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage.Extensions;
using Files.Shared.Utils;
using System.Windows.Input;

namespace Files.App.Data.Items;

public sealed partial class WidgetFileTagCardItem : WidgetCardItem
{
    private IFolderViewViewModel FolderViewViewModel { get; }

	// Dependency injections

	private IContentPageContext ContentPageContext { get; }

	// Fields

	private readonly IStorable _associatedStorable;

	// Properties

	public bool IsFolder
		=> _associatedStorable is IFolder;

	private IImage? _Icon;
	public IImage? Icon
	{
		get => _Icon;
		set => SetProperty(ref _Icon, value);
	}

	private string? _Name;
	public string? Name
	{
		get => _Name;
		set => SetProperty(ref _Name, value);
	}

	private string? _Path;
	public override string? Path
	{
		get => _Path;
		set => SetProperty(ref _Path, value);
	}

	// Commands

	public ICommand ClickCommand { get; }

	public WidgetFileTagCardItem(IFolderViewViewModel folderViewViewModel, IStorable associatedStorable, IImage? icon)
	{
        FolderViewViewModel = folderViewViewModel;
        ContentPageContext = folderViewViewModel.GetRequiredService<IContentPageContext>();

        _associatedStorable = associatedStorable;
		_Icon = icon;
		_Name = associatedStorable.Name;
		_Path = associatedStorable.TryGetPath();
		Item = this;

		ClickCommand = new AsyncRelayCommand(ClickAsync);
	}

	private Task ClickAsync()
	{
		return NavigationHelpers.OpenPath(FolderViewViewModel, _associatedStorable.Id, ContentPageContext.ShellPage!);
	}
}
