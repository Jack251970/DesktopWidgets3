// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.Widgets;

public sealed partial class FileTagsWidgetViewModel : ObservableObject
{
	private readonly Func<string, Task> _openAction;

	private IFileTagsService FileTagsService { get; } = DependencyExtensions.GetService<IFileTagsService>();

	public ObservableCollection<FileTagsContainerViewModel> Containers { get; }

	public FileTagsWidgetViewModel(Func<string, Task> openAction)
	{
		_openAction = openAction;
		Containers = new();
	}

	/// <inheritdoc/>
	public async Task InitAsync(IFolderViewViewModel folderViewViewModel, CancellationToken cancellationToken = default)
	{
		await foreach (var item in FileTagsService.GetTagsAsync(cancellationToken))
		{
			var container = new FileTagsContainerViewModel(folderViewViewModel, item.Uid, _openAction)
			{
				Name = item.Name,
				Color = item.Color
			};
			Containers.Add(container);

			_ = container.InitAsync(cancellationToken);
		}
	}
}
