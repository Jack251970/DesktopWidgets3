// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services;

#pragma warning disable IL2026 // Unrecognized escape sequence in cref attribute

public class QuickAccessService : IQuickAccessService
{
	private static readonly string guid = "::{679f85cb-0220-4080-b29b-5540cc05aab6}";

	public async Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync()
	{
		var result = (await Win32Shell.GetShellFolderAsync(guid, "Enumerate", 0, int.MaxValue, "System.Home.IsPinned")).Enumerate
			.Where(link => link.IsFolder);
		return result;
	}

	public Task PinToSidebarAsync(string folderPath) => PinToSidebarAsync(new[] { folderPath });

	public Task PinToSidebarAsync(string[] folderPaths) => PinToSidebarAsync(folderPaths, true);

	private async Task PinToSidebarAsync(string[] folderPaths, bool doUpdateQuickAccessWidget)
	{
		foreach (var folderPath in folderPaths)
        {
            await ContextMenu.InvokeVerb("pintohome", new[] {folderPath});
        }

        await DependencyExtensions.GetService<QuickAccessManager>().Model.LoadAsync();
		/*if (doUpdateQuickAccessWidget)
        {
            DependencyExtensions.GetService<QuickAccessManager>().UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, true));
        }*/
    }

	public Task UnpinFromSidebarAsync(string folderPath) => UnpinFromSidebarAsync(new[] { folderPath }); 

	public Task UnpinFromSidebarAsync(string[] folderPaths) => UnpinFromSidebarAsync(folderPaths, true);

	private async Task UnpinFromSidebarAsync(string[] folderPaths, bool doUpdateQuickAccessWidget)
	{
		var shellAppType = Type.GetTypeFromProgID("Shell.Application");
		var shell = Activator.CreateInstance(shellAppType);
		dynamic? f2 = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { $"shell:{guid}" });

		if (folderPaths.Length == 0)
        {
            folderPaths = (await GetPinnedFoldersAsync())
				.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
				.Select(link => link.FilePath).ToArray();
        }

        foreach (var fi in f2!.Items())
		{
			if (ShellStorageFolder.IsShellPath((string)fi.Path))
			{
				var folder = await ShellStorageFolder.FromPathAsync((string)fi.Path);
				var path = folder?.Path;

				if (path is not null && 
					(folderPaths.Contains(path) || (path.StartsWith(@"\\SHELL\") && folderPaths.Any(x => x.StartsWith(@"\\SHELL\"))))) // Fix for the Linux header
				{
					await SafetyExtensions.IgnoreExceptions(async () =>
					{
						await fi.InvokeVerb("unpinfromhome");
					});
					continue;
				}
			}

			if (folderPaths.Contains((string)fi.Path))
			{
				await SafetyExtensions.IgnoreExceptions(async () =>
				{
					await fi.InvokeVerb("unpinfromhome");
				});
			}
		}

		await DependencyExtensions.GetService<QuickAccessManager>().Model.LoadAsync();
		/*if (doUpdateQuickAccessWidget)
        {
            DependencyExtensions.GetService<QuickAccessManager>().UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(folderPaths, false));
        }*/
    }

	public bool IsItemPinned(string folderPath)
	{
		return DependencyExtensions.GetService<QuickAccessManager>().Model.FavoriteItems.Contains(folderPath);
	}

	public async Task SaveAsync(string[] items)
	{
		if (Equals(items, DependencyExtensions.GetService<QuickAccessManager>().Model.FavoriteItems.ToArray()))
        {
            return;
        }

        DependencyExtensions.GetService<QuickAccessManager>().PinnedItemsWatcher!.EnableRaisingEvents = false;

		// Unpin every item that is below this index and then pin them all in order
		await UnpinFromSidebarAsync(Array.Empty<string>(), false);

		await PinToSidebarAsync(items, false);
        DependencyExtensions.GetService<QuickAccessManager>().PinnedItemsWatcher!.EnableRaisingEvents = true;

        /*DependencyExtensions.GetService<QuickAccessManager>().UpdateQuickAccessWidget?.Invoke(this, new ModifyQuickAccessEventArgs(items, true)
		{
			Reorder = true
		});*/
	}
}
