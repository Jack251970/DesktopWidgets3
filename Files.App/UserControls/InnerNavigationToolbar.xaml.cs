// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls;

public sealed partial class InnerNavigationToolbar : UserControl
{
	public InnerNavigationToolbar()
	{
		InitializeComponent();

        // CHANGE: Remove all secondary commands.
        BaseCommandBar.SecondaryCommands.Clear();
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        UserSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
        Commands = folderViewViewModel.GetService<ICommandManager>();
        ModifiableCommands = folderViewViewModel.GetService<IModifiableCommandManager>();
    }

    public IUserSettingsService UserSettingsService { get; set; } = null!;
	public ICommandManager Commands { get; set; } = null!;
    public IModifiableCommandManager ModifiableCommands { get; set; } = null!;

    private readonly IAddItemService addItemService = DependencyExtensions.GetService<IAddItemService>();

	public static AppModel AppModel => App.AppModel;

	public ToolbarViewModel? ViewModel
	{
		get => (ToolbarViewModel)GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	// Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty ViewModelProperty =
		DependencyProperty.Register(nameof(ViewModel), typeof(ToolbarViewModel), typeof(InnerNavigationToolbar), new PropertyMetadata(null));

	public bool ShowViewControlButton
    {
        get => (bool)GetValue(ShowViewControlButtonProperty);
        set => SetValue(ShowViewControlButtonProperty, value);
    }

    // Using a DependencyProperty as the backing store for ShowViewControlButton.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowViewControlButtonProperty =
		DependencyProperty.Register("ShowViewControlButton", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));

	public bool ShowPreviewPaneButton
    {
        get => (bool)GetValue(ShowPreviewPaneButtonProperty);
        set => SetValue(ShowPreviewPaneButtonProperty, value);
    }

    // Using a DependencyProperty as the backing store for ShowPreviewPaneButton.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShowPreviewPaneButtonProperty =
		DependencyProperty.Register("ShowPreviewPaneButton", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));
	private void NewEmptySpace_Opening(object sender, object e)
	{
		var shell = NewEmptySpace.Items.Where(x => (x.Tag as string) == "CreateNewFile").Reverse().ToList();
		shell.ForEach(x => NewEmptySpace.Items.Remove(x));
		if (!ViewModel!.InstanceViewModel.CanCreateFileInPage)
        {
            return;
        }

        var cachedNewContextMenuEntries = addItemService.GetEntries();
		if (cachedNewContextMenuEntries is null)
        {
            return;
        }

        var separatorIndex = NewEmptySpace.Items.IndexOf(NewEmptySpace.Items.Single(x => x.Name == "NewMenuFileFolderSeparator"));

		ushort key = 0;
		var keyFormat = $"D{cachedNewContextMenuEntries.Count.ToString().Length}";

		foreach (var newEntry in Enumerable.Reverse(cachedNewContextMenuEntries))
		{
			MenuFlyoutItem menuLayoutItem;
			if (!string.IsNullOrEmpty(newEntry.IconBase64))
			{
				var bitmapData = Convert.FromBase64String(newEntry.IconBase64);
				using var ms = new MemoryStream(bitmapData);
				var image = new BitmapImage();
				_ = image.SetSourceAsync(ms.AsRandomAccessStream());
				menuLayoutItem = new MenuFlyoutItemWithImage()
				{
					Text = newEntry.Name,
					BitmapIcon = image,
					Tag = "CreateNewFile"
				};
			}
			else
			{
				menuLayoutItem = new MenuFlyoutItem()
				{
					Text = newEntry.Name,
					Icon = new FontIcon
					{
						Glyph = "\xE7C3"
					},
					Tag = "CreateNewFile"
				};
			}
			menuLayoutItem.AccessKey = (cachedNewContextMenuEntries.Count + 1 - (++key)).ToString(keyFormat);
			menuLayoutItem.Command = ViewModel.CreateNewFileCommand;
			menuLayoutItem.CommandParameter = newEntry;
			NewEmptySpace.Items.Insert(separatorIndex + 1, menuLayoutItem);
		}
	}

	private void SortGroup_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
	{
		if (sender is MenuFlyoutSubItem menu)
		{
			var items = menu.Items
				.TakeWhile(item => item is not MenuFlyoutSeparator)
				.Where(item => item.IsEnabled)
				.ToList();

			var format = $"D{items.Count.ToString().Length}";

			for (ushort index = 0; index < items.Count; ++index)
			{
				items[index].AccessKey = (index+1).ToString(format);
			}
		}

	}
}