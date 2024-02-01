// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Immutable;
using Windows.Foundation.Metadata;

namespace Files.App.Dialogs;

public sealed partial class CreateArchiveDialog : ContentDialog
{
    private readonly IFolderViewViewModel FolderViewViewModel;

	private bool canCreate = false;
	public bool CanCreate => canCreate;

	public string FileName
	{
		get => ViewModel.FileName;
		set => ViewModel.FileName = value;
	}

	public bool UseEncryption
	{
		get => ViewModel.UseEncryption;
		set => ViewModel.UseEncryption = value;
	}

	public string Password
	{
		get => ViewModel.Password;
		set => ViewModel.Password = value;
	}

	public ArchiveFormats FileFormat
	{
		get => ViewModel.FileFormat.Key;
		set => ViewModel.FileFormat = ViewModel.FileFormats.First(format => format.Key == value);
	}

	public ArchiveCompressionLevels CompressionLevel
	{
		get => ViewModel.CompressionLevel.Key;
		set => ViewModel.CompressionLevel = ViewModel.CompressionLevels.First(level => level.Key == value);
	}

	public ArchiveSplittingSizes SplittingSize
	{
		get => ViewModel.SplittingSize.Key;
		set => ViewModel.SplittingSize = ViewModel.SplittingSizes.First(size => size.Key == value);
	}

	private DialogViewModel ViewModel { get; set; } = null!;

	public CreateArchiveDialog(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

        InitializeComponent();

        ViewModel = new(folderViewViewModel);
		ViewModel.PropertyChanged += ViewModel_PropertyChanged;
	}

	public new Task<ContentDialogResult> ShowAsync()
	{
		return SetContentDialogRoot(FolderViewViewModel, this).ShowAsync().AsTask();
	}

	private static ContentDialog SetContentDialogRoot(IFolderViewViewModel folderViewViewModel, ContentDialog contentDialog)
	{
		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            contentDialog.XamlRoot = folderViewViewModel.MainWindow.Content.XamlRoot; // WinUi3
        }

        return contentDialog;
	}

	private void ContentDialog_Loaded(object _, RoutedEventArgs e)
	{
		Loaded -= ContentDialog_Loaded;

		FileNameBox.SelectionStart = FileNameBox.Text.Length;
		FileNameBox.Focus(FocusState.Programmatic);
	}
	private void ContentDialog_Closing(ContentDialog _, ContentDialogClosingEventArgs e)
	{
		InvalidNameWarning.IsOpen = false;
		Closing -= ContentDialog_Closing;
		ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

		if (e.Result is ContentDialogResult.Primary)
			canCreate = true;
	}

	private void PasswordBox_Loading(FrameworkElement _, object e)
		=> PasswordBox.Focus(FocusState.Programmatic);

	private void ViewModel_PropertyChanged(object? _, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(DialogViewModel.UseEncryption) && ViewModel.UseEncryption)
        {
            PasswordBox.Focus(FocusState.Programmatic);
        }
    }

	private class DialogViewModel : ObservableObject
	{
        private readonly IFolderViewViewModel FolderViewViewModel;

		public bool IsNameValid => FilesystemHelpers.IsValidForFilename(FolderViewViewModel, fileName);

		public bool ShowNameWarning => !string.IsNullOrEmpty(fileName) && !IsNameValid;

		private string fileName = string.Empty;
		public string FileName
		{
			get => fileName;
			set
			{
				if (SetProperty(ref fileName, value))
				{
					OnPropertyChanged(nameof(IsNameValid));
					OnPropertyChanged(nameof(ShowNameWarning));
				}
			}
		}

		private FileFormatItem fileFormat;
		public FileFormatItem FileFormat
		{
			get => fileFormat;
			set
			{
				if (SetProperty(ref fileFormat, value))
                {
                    OnPropertyChanged(nameof(CanSplit));
                }
            }
		}

		private CompressionLevelItem compressionLevel;
		public CompressionLevelItem CompressionLevel
		{
			get => compressionLevel;
			set => SetProperty(ref compressionLevel, value);
		}

		public bool CanSplit => FileFormat.Key is ArchiveFormats.SevenZip;

		private SplittingSizeItem splittingSize;
		public SplittingSizeItem SplittingSize
		{
			get => splittingSize;
			set => SetProperty(ref splittingSize, value);
		}

		private bool useEncryption = false;
		public bool UseEncryption
		{
			get => useEncryption;
			set
			{
				if (SetProperty(ref useEncryption, value) && !useEncryption)
                {
                    Password = string.Empty;
                }
            }
		}

		private string password = string.Empty;
		public string Password
		{
			get => password;
			set
			{
				if (SetProperty(ref password, value) && !string.IsNullOrEmpty(password))
                {
                    UseEncryption = true;
                }
            }
		}

		public IImmutableList<FileFormatItem> FileFormats { get; } = new List<FileFormatItem>
		{
			new(ArchiveFormats.Zip, ".zip"),
			new(ArchiveFormats.SevenZip, ".7z"),
		}.ToImmutableList();

		public IImmutableList<CompressionLevelItem> CompressionLevels { get; } = new List<CompressionLevelItem>
		{
			new(ArchiveCompressionLevels.Ultra, "CompressionLevelUltra".GetLocalizedResource()),
			new(ArchiveCompressionLevels.High, "CompressionLevelHigh".GetLocalizedResource()),
			new(ArchiveCompressionLevels.Normal, "CompressionLevelNormal".GetLocalizedResource()),
			new(ArchiveCompressionLevels.Low, "CompressionLevelLow".GetLocalizedResource()),
			new(ArchiveCompressionLevels.Fast, "CompressionLevelFast".GetLocalizedResource()),
			new(ArchiveCompressionLevels.None, "CompressionLevelNone".GetLocalizedResource()),
		}.ToImmutableList();

		public IImmutableList<SplittingSizeItem> SplittingSizes { get; } = new List<SplittingSizeItem>
		{
			new(ArchiveSplittingSizes.None, "Do not split".GetLocalizedResource()),
			new(ArchiveSplittingSizes.Mo10, ToSizeText(10)),
			new(ArchiveSplittingSizes.Mo100, ToSizeText(100)),
			new(ArchiveSplittingSizes.Cd650, ToSizeText(650), "CD".GetLocalizedResource()),
			new(ArchiveSplittingSizes.Cd700, ToSizeText(700), "CD".GetLocalizedResource()),
			new(ArchiveSplittingSizes.Mo1024, ToSizeText(1024)),
			new(ArchiveSplittingSizes.Mo2048, ToSizeText(2048)),
			new(ArchiveSplittingSizes.Fat4092, ToSizeText(4092), "FAT".GetLocalizedResource()),
			new(ArchiveSplittingSizes.Dvd4480, ToSizeText(4480), "DVD".GetLocalizedResource()),
			new(ArchiveSplittingSizes.Mo5120, ToSizeText(5120)),
			new(ArchiveSplittingSizes.Dvd8128, ToSizeText(8128), "DVD".GetLocalizedResource()),
			new(ArchiveSplittingSizes.Bd23040, ToSizeText(23040), "Bluray".GetLocalizedResource()),
		}.ToImmutableList();

		public DialogViewModel(IFolderViewViewModel folderViewViewModel)
		{
            FolderViewViewModel = folderViewViewModel;

			fileFormat = FileFormats.First(format => format.Key is ArchiveFormats.Zip);
			compressionLevel = CompressionLevels.First(level => level.Key is ArchiveCompressionLevels.Normal);
			splittingSize = SplittingSizes.First(size => size.Key is ArchiveSplittingSizes.None);
		}

		private static string ToSizeText(ulong megaBytes) => ByteSize.FromMebiBytes(megaBytes).ShortString;

		public record FileFormatItem(ArchiveFormats Key, string Label);

		public record CompressionLevelItem(ArchiveCompressionLevels Key, string Label);
	}
}

internal record SplittingSizeItem(ArchiveSplittingSizes Key, string Label, string Description = "")
{
	public string Separator => string.IsNullOrEmpty(Description) ? string.Empty : "-";
}
