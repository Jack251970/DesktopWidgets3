// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;

namespace Files.App.ViewModels.Previews;

public class HtmlPreviewViewModel(ListedItem item) : BasePreviewModel(item)
{
    public static bool ContainsExtension(string extension)
		=> extension is ".htm" or ".html" or ".svg";

    public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
    {
        await Task.CompletedTask;
        return [];
    }
}
