// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;

namespace Files.App.ViewModels.Previews;

public sealed class MarkdownPreviewViewModel(ListedItem item) : BasePreviewModel(item)
{
	private string textValue = null!;
	public string TextValue
	{
		get => textValue;
		private set => SetProperty(ref textValue, value);
	}

    public static bool ContainsExtension(string extension)
		=> extension is ".md" or ".markdown";

	public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
	{
		var text = await ReadFileAsTextAsync(Item.ItemFile);
		TextValue = text.Left(Constants.PreviewPane.TextCharacterLimit);

		return [];
	}
}
