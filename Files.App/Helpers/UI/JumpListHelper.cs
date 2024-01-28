// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;

namespace Files.App.Helpers;

public sealed class JumpListHelper
{
	private static readonly IJumpListService jumpListService = DependencyExtensions.GetService<IJumpListService>();

	public static async Task InitializeUpdatesAsync()
	{
		try
		{
			/*DependencyExtensions.GetService<QuickAccessManager>().UpdateQuickAccessWidget -= UpdateQuickAccessWidgetAsync;
            DependencyExtensions.GetService<QuickAccessManager>().UpdateQuickAccessWidget += UpdateQuickAccessWidgetAsync;*/

			await jumpListService.RefreshPinnedFoldersAsync();
		}
		catch (Exception ex)
		{
			DependencyExtensions.GetService<ILogger>()?.LogWarning(ex, ex.Message);
		}
	}

	/*private static async void UpdateQuickAccessWidgetAsync(object? sender, ModifyQuickAccessEventArgs e)
	{
		await jumpListService.RefreshPinnedFoldersAsync();
	}*/
}