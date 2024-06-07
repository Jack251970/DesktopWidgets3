// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.ViewModels.Previews;

public abstract class BasePreviewModel : ObservableObject
{
    protected readonly IFolderViewViewModel FolderViewViewModel;

    private readonly IUserSettingsService userSettingsService;

    public ListedItem Item { get; }

	private BitmapImage fileImage = null!;
	public BitmapImage FileImage
	{
		get => fileImage;
		protected set => SetProperty(ref fileImage, value);
	}

    public List<FileProperty> DetailsFromPreview { get; set; } = null!;

	/// <summary>
	/// This is cancelled when the user has selected another file or closed the pane.
	/// </summary>
	public CancellationTokenSource LoadCancelledTokenSource { get; } = new CancellationTokenSource();

    public BasePreviewModel(ListedItem item) : base()
    {
        Item = item;

        FolderViewViewModel = item.FolderViewViewModel;
        userSettingsService = FolderViewViewModel.GetService<IUserSettingsService>();
    }

    public delegate void LoadedEventHandler(object sender, EventArgs e);

	public static Task LoadDetailsOnlyAsync(ListedItem item, List<FileProperty> details = null!)
	{
		var temp = new DetailsOnlyPreviewModel(item) { DetailsFromPreview = details };
		return temp.LoadAsync();
	}

	public static Task<string> ReadFileAsTextAsync(BaseStorageFile file, int maxLength = 10 * 1024 * 1024)
		=> file.ReadTextAsync(maxLength);

	/// <summary>
	/// Call this function when you are ready to load the preview and details.
	/// Override if you need custom loading code.
	/// </summary>
	/// <returns>The task to run</returns>
	public async virtual Task LoadAsync()
	{
		List<FileProperty> detailsFull = [];

		if (Item.ItemFile is null)
		{
			var rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(Item.ItemPath));
			Item.ItemFile = await StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath, rootItem);
		}

		await Task.Run(async () =>
		{
			DetailsFromPreview = await LoadPreviewAndDetailsAsync();
            if (userSettingsService.InfoPaneSettingsService.SelectedTab == InfoPaneTabs.Details)
			{
				// Add the details from the preview function, then the system file properties
				DetailsFromPreview?.ForEach(i => detailsFull.Add(i));
				var props = await GetSystemFilePropertiesAsync();
				if (props is not null)
				{
					detailsFull.AddRange(props);
				}
			}
        });

		Item.FileDetails = new ObservableCollection<FileProperty>(detailsFull);
	}

    /// <summary>
    /// Override this and place the code to load the file preview here.
    /// You can return details that may have been obtained while loading the preview (eg. word count).
    /// This details will be displayed *before* the system file properties.
    /// If there are none, return an empty list.
    /// </summary>
    /// <returns>A list of details</returns>
    public async virtual Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
    {
        var result = await FileThumbnailHelper.GetIconAsync(
            Item.ItemPath,
            Constants.ShellIconSizes.Jumbo,
            false,
            IconOptions.None);

        if (result is not null)
        {
            await ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(async () => FileImage = (await result.ToBitmapAsync())!);
        }
        else
        {
            FileImage ??= (await ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(() => new BitmapImage()))!;
        }

        return [];
    }

    /// <summary>
    /// Override this if the preview control needs to handle the unloaded event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public virtual void PreviewControlBase_Unloaded(object sender, RoutedEventArgs e)
		=> LoadCancelledTokenSource.Cancel();

	protected FileProperty GetFileProperty(string nameResource, object value)
		=> new(FolderViewViewModel) { NameResource = nameResource, Value = value };

	private async Task<List<FileProperty>> GetSystemFilePropertiesAsync()
	{
		if (Item.IsShortcut)
        {
            return null!;
        }

        var list = await FileProperty.RetrieveAndInitializePropertiesAsync(
            FolderViewViewModel,
            Item.ItemFile,
			Constants.ResourceFilePaths.PreviewPaneDetailsPropertiesJsonUriPath);

		list.Find(x => x.ID is "address")!.Value = await LocationHelpers.GetAddressFromCoordinatesAsync(
            (double?)list.Find(x => x.Property is "System.GPS.LatitudeDecimal")!.Value,
			(double?)list.Find(x => x.Property is "System.GPS.LongitudeDecimal")!.Value
        );

        // Adds the value for the file tag
        list.FirstOrDefault(x => x.ID is "filetag")!.Value =
			Item.FileTagsUI is not null ? string.Join(',', Item.FileTagsUI.Select(x => x.Name)) : null!;

		return list.Where(i => i.ValueText is not null).ToList();
	}

	private sealed class DetailsOnlyPreviewModel(ListedItem item) : BasePreviewModel(item)
	{
        public override Task<List<FileProperty>> LoadPreviewAndDetailsAsync() => Task.FromResult(DetailsFromPreview);
	}
}
