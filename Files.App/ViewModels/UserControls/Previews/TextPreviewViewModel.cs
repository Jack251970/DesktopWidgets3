// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.FilePreviews;
using Files.App.ViewModels.Properties;

namespace Files.App.ViewModels.Previews;

public sealed class TextPreviewViewModel(ListedItem item) : BasePreviewModel(item)
{
	private string textValue = null!;
    public string TextValue
    {
        get => textValue;
        private set => SetProperty(ref textValue, value);
    }

    public static bool ContainsExtension(string extension)
		=> extension is ".txt";

    private static readonly char[] separator = [' ', '\n'];

    public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
	{
		var details = new List<FileProperty>();

		try
		{
			var text = TextValue ?? await ReadFileAsTextAsync(Item.ItemFile);

			details.Add(GetFileProperty("PropertyLineCount", text.Split('\n').Length));
			details.Add(GetFileProperty("PropertyWordCount", text.Split(separator, StringSplitOptions.RemoveEmptyEntries).Length));

			TextValue = text.Left(Constants.PreviewPane.TextCharacterLimit);
		}
		catch (Exception e)
		{
			Debug.WriteLine(e);
		}

		return details;
	}

	public static async Task<TextPreview> TryLoadAsTextAsync(ListedItem item)
	{
		var extension = item.FileExtension?.ToLowerInvariant();
		if (ExcludedExtensions(extension!) || item.FileSizeBytes is 0 or > Constants.PreviewPane.TryLoadAsTextSizeLimit)
        {
            return null!;
        }

        try
		{
			item.ItemFile = await StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath);

			var text = await ReadFileAsTextAsync(item.ItemFile);
			var isBinaryFile = text.Contains("\0\0\0\0", StringComparison.Ordinal);

			if (isBinaryFile)
            {
                return null!;
            }

            var model = new TextPreviewViewModel(item) { TextValue = text };
			await model.LoadAsync();

			return new TextPreview(model);
		}
		catch
		{
			return null!;
		}
	}

	private static bool ExcludedExtensions(string extension)
		=> extension is ".iso";
}
