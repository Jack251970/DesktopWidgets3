using Windows.UI.StartScreen;

namespace Files.App.Services;

/// <inheritdoc cref="IStartMenuService"/>
public sealed class StartMenuService : IStartMenuService
{
	[Obsolete("See IStartMenuService for further information.")]
    public bool IsPinned(string folderPath)
    {
        var tileId = GetNativeTileId(folderPath);
        return SecondaryTile.Exists(tileId);
    }

    /// <inheritdoc/>
    public Task<bool> IsPinnedAsync(IStorable storable)
    {
        var tileId = GetNativeTileId(storable.Id);
        var exists = SecondaryTile.Exists(tileId);

        return Task.FromResult(exists);
    }

    /// <inheritdoc/>
	public async Task PinAsync(IFolderViewViewModel folderViewViewModel, IStorable storable, string? displayName = null)
	{
        var tileId = GetNativeTileId(storable.Id);
        displayName ??= storable.Name;

        try
        {
            var path150x150 = new Uri("ms-appx:///Assets/tile-0-300x300.png");
            var path71x71 = new Uri("ms-appx:///Assets/tile-0-250x250.png");

            var tile = new SecondaryTile(
                tileId,
                displayName,
                storable.Id,
                path150x150,
                TileSize.Square150x150)
            {
                VisualElements =
                    {
                        Square71x71Logo = path71x71,
                        ShowNameOnSquare150x150Logo = true
                    }
            };

            WinRT.Interop.InitializeWithWindow.Initialize(tile, folderViewViewModel.WindowHandle);

            await tile.RequestCreateAsync();
        }
        catch (Exception e)
        {
            Debug.WriteLine(tileId);
            Debug.WriteLine(e.ToString());
        }
    }

    /// <inheritdoc/>
    public async Task UnpinAsync(IStorable storable)
    {
        var startScreen = StartScreenManager.GetDefault();
        var tileId = GetNativeTileId(storable.Id);

        await startScreen.TryRemoveSecondaryTileAsync(tileId);
    }

    private static string GetNativeTileId(string id)
	{
		// Remove symbols because windows doesn't like them in the ID, and will blow up
		var str = $"folder-{new string(id.Where(char.IsLetterOrDigit).ToArray())}";

		// If the id string is too long, Windows will throw an error, so remove every other character
		if (str.Length > 64)
        {
            str = new string(str.Where((_, i) => i % 2 == 0).ToArray());
        }

        return str;
	}
}
