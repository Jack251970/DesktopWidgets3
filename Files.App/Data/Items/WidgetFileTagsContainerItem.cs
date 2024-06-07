// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Utils;
using System.Windows.Input;

namespace Files.App.Data.Items;

public sealed partial class WidgetFileTagsContainerItem : ObservableObject, IAsyncInitialize
{
    private readonly IFolderViewViewModel FolderViewViewModel;

	// Fields

	private readonly IFileTagsService FileTagsService = DependencyExtensions.GetService<IFileTagsService>();
	private readonly IImageService ImageService = DependencyExtensions.GetService<IImageService>();
	private readonly ICommandManager Commands;
	private readonly IContentPageContext ContentPageContext ;

	private readonly string _tagUid;

	// Properties

	public ObservableCollection<WidgetFileTagCardItem> Tags { get; }

	private string? _Color;
	public string? Color
	{
		get => _Color;
		set => SetProperty(ref _Color, value);
	}

	private string? _Name;
	public string? Name
	{
		get => _Name;
		set => SetProperty(ref _Name, value);
	}

	// Events

	public delegate void SelectedTagChangedEventHandler(object sender, SelectedTagChangedEventArgs e);
	public static event SelectedTagChangedEventHandler? SelectedTagChanged;

	// Commands

	public ICommand ViewMoreCommand { get; }
	public ICommand OpenAllCommand { get; }

	public WidgetFileTagsContainerItem(IFolderViewViewModel folderViewViewModel, string tagUid)
	{
        FolderViewViewModel = folderViewViewModel;
        Commands = folderViewViewModel.GetService<ICommandManager>();
        ContentPageContext = folderViewViewModel.GetService<IContentPageContext>();

		_tagUid = tagUid;
		Tags = [];

		ViewMoreCommand = new AsyncRelayCommand(ViewMore);
		OpenAllCommand = new AsyncRelayCommand(OpenAll);
	}

	/// <inheritdoc/>
	public async Task InitAsync(CancellationToken cancellationToken = default)
	{
		await foreach (var item in FileTagsService.GetItemsForTagAsync(_tagUid))
		{
			var icon = await ImageService.GetIconAsync(item.Storable, default);
			Tags.Add(new(FolderViewViewModel, item.Storable, icon));
		}
	}

	private Task<bool> ViewMore()
	{
		return NavigationHelpers.OpenPath(FolderViewViewModel, $"tag:{Name}", ContentPageContext.ShellPage!);
	}

	private Task OpenAll()
	{
		SelectedTagChanged?.Invoke(this, new(Tags.Select(tag => (tag.Path, tag.IsFolder))!));

		return Commands.OpenAllTaggedItems.ExecuteAsync();
	}
}
