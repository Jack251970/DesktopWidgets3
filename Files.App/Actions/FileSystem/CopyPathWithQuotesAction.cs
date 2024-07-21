// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions;

internal sealed class CopyPathWithQuotesAction(IContentPageContext context) : IAction
{
	private readonly IContentPageContext context = context;

	public string Label
		=> "CopyPathWithQuotes".GetLocalizedResource();

	public string Description
		=> "CopyPathWithQuotesDescription".GetLocalizedResource();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconCopyPath");

	public HotKey HotKey
		=> new(Keys.C, KeyModifiers.CtrlAlt);

	public bool IsExecutable
		=> context.HasSelection;

    public Task ExecuteAsync(object? parameter = null)
	{
		if (context.ShellPage?.SlimContentPage is not null)
		{
			var selectedItems = context.ShellPage.SlimContentPage.SelectedItems;
			var path = selectedItems is not null
				? string.Join("\n", selectedItems.Select(item => $"\"{item.ItemPath}\""))
				: context.ShellPage.ShellViewModel.WorkingDirectory;

			if (FtpHelpers.IsFtpPath(path))
            {
                path = path.Replace("\\", "/", StringComparison.Ordinal);
            }

            SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage data = new();
				data.SetText(path);

				Clipboard.SetContent(data);
				Clipboard.Flush();
			});
		}

		return Task.CompletedTask;
	}
}
