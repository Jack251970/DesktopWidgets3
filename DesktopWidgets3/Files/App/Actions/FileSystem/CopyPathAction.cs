// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Commands;
using Files.App.Utils.Storage;
using Files.Shared.Extensions;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions;

internal class CopyPathAction : IAction
{
    private readonly FolderViewViewModel context;

    public string Label
		=> "CopyPath".ToLocalized();

	public string Description
		=> "CopyPathDescription".ToLocalized();

	public RichGlyph Glyph
		=> new(opacityStyle: "ColorIconCopyPath");

	/*public HotKey HotKey
		=> new(Keys.C, KeyModifiers.CtrlShift);*/

	public bool IsExecutable
		=> context.HasSelection;

	public CopyPathAction(FolderViewViewModel viewModel)
    {
        context = viewModel;
	}

	public Task ExecuteAsync()
	{
		if (context is not null)
		{
			var path = context.SelectedItems is not null
				? context.SelectedItems.Select(x => x.ItemPath).Aggregate((accum, current) => accum + "\n" + current)
				: context.FileSystemViewModel.WorkingDirectory;

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
