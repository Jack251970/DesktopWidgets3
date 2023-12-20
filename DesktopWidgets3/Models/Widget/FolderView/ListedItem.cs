// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Utils.Storage;
using Files.App.ViewModels.Properties;
using Files.Core.Services.DateTimeFormatter;
using Files.Shared.Helpers;
using FluentFTP;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;

namespace DesktopWidgets3.Models.Widget.FolderView;

public class ListedItem : ObservableObject, IGroupableItem
{
    protected static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetRequiredService<IDateTimeFormatter>();

    public bool IsHiddenItem { get; set; } = false;

    public StorageItemTypes PrimaryItemAttribute
    {
        get; set;
    }

    private volatile int itemPropertiesInitialized = 0;
    public bool ItemPropertiesInitialized
    {
        get => itemPropertiesInitialized == 1;
        set => Interlocked.Exchange(ref itemPropertiesInitialized, value ? 1 : 0);
    }

    /*public string ItemTooltipText
    {
        get
        {
            var tooltipBuilder = new StringBuilder();
            tooltipBuilder.AppendLine($"{"NameWithColon".GetLocalizedResource()} {Name}");
            tooltipBuilder.AppendLine($"{"ItemType".GetLocalizedResource()} {itemType}");
            tooltipBuilder.Append($"{"ToolTipDescriptionDate".GetLocalizedResource()} {ItemDateModified}");
            if (!string.IsNullOrWhiteSpace(FileSize))
            {
                tooltipBuilder.Append($"{Environment.NewLine}{"SizeLabel".GetLocalizedResource()} {FileSize}");
            }

            if (SyncStatusUI.LoadSyncStatus)
            {
                tooltipBuilder.Append($"{Environment.NewLine}{"syncStatusColumn/Header".GetLocalizedResource()}: {syncStatusUI.SyncStatusString}");
            }

            return tooltipBuilder.ToString();
        }
    }*/

    public string FolderRelativeId
    {
        get; set;
    }

    public bool ContainsFilesOrFolders { get; set; } = true;

    private bool needsPlaceholderGlyph = true;
    public bool NeedsPlaceholderGlyph
    {
        get => needsPlaceholderGlyph;
        set => SetProperty(ref needsPlaceholderGlyph, value);
    }

    private bool loadFileIcon;
    public bool LoadFileIcon
    {
        get => loadFileIcon;
        set => SetProperty(ref loadFileIcon, value);
    }

    private bool loadWebShortcutGlyph;
    public bool LoadWebShortcutGlyph
    {
        get => loadWebShortcutGlyph;
        set => SetProperty(ref loadWebShortcutGlyph, value);
    }

    /*private CloudDriveSyncStatusUI syncStatusUI = new();
    public CloudDriveSyncStatusUI SyncStatusUI
    {
        get => syncStatusUI;
        set
        {
            // For some reason this being null will cause a crash with bindings
            value ??= new CloudDriveSyncStatusUI();
            if (SetProperty(ref syncStatusUI, value))
            {
                OnPropertyChanged(nameof(SyncStatusString));
                OnPropertyChanged(nameof(ItemTooltipText));
            }
        }
    }

    // This is used to avoid passing a null value to AutomationProperties.Name, which causes a crash
    public string SyncStatusString => string.IsNullOrEmpty(SyncStatusUI?.SyncStatusString) ? "CloudDriveSyncStatus_Unknown".GetLocalizedResource() : SyncStatusUI.SyncStatusString;*/

    private BitmapImage fileImage;
    public BitmapImage FileImage
    {
        get => fileImage;
        set
        {
            if (SetProperty(ref fileImage, value))
            {
                if (value is not null)
                {
                    LoadFileIcon = true;
                    NeedsPlaceholderGlyph = false;
                    LoadWebShortcutGlyph = false;
                }
            }
        }
    }

    private BitmapImage iconOverlay;
    public BitmapImage IconOverlay
    {
        get => iconOverlay;
        set
        {
            if (value is not null)
            {
                SetProperty(ref iconOverlay, value);
            }
        }
    }

    private BitmapImage shieldIcon;
    public BitmapImage ShieldIcon
    {
        get => shieldIcon;
        set
        {
            if (value is not null)
            {
                SetProperty(ref shieldIcon, value);
            }
        }
    }

    private string itemPath;
    public string ItemPath
    {
        get => itemPath;
        set => SetProperty(ref itemPath, value);
    }

    private string itemNameRaw;
    public string ItemNameRaw
    {
        get => itemNameRaw;
        set
        {
            if (SetProperty(ref itemNameRaw, value))
            {
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public virtual string Name
    {
        get
        {
            if (PrimaryItemAttribute == StorageItemTypes.File)
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(itemNameRaw);
                if (!string.IsNullOrEmpty(nameWithoutExtension))
                {
                    return nameWithoutExtension;
                }
            }
            return itemNameRaw;
        }
    }

    private string itemType;
    public string ItemType
    {
        get => itemType;
        set
        {
            if (value is not null)
            {
                SetProperty(ref itemType, value);
            }
        }
    }

    public string FileExtension
    {
        get; set;
    }

    private string fileSize;
    public string FileSize
    {
        get => fileSize;
        set
        {
            SetProperty(ref fileSize, value);
            OnPropertyChanged(nameof(FileSizeDisplay));
        }
    }

    public string FileSizeDisplay => string.IsNullOrEmpty(FileSize) ? "ItemSizeNotCalculated".GetLocalizedResource() : FileSize;

    public long FileSizeBytes
    {
        get; set;
    }

    public string ItemDateModified
    {
        get; private set;
    }

    public string ItemDateCreated
    {
        get; private set;
    }

    public string ItemDateAccessed
    {
        get; private set;
    }

    private DateTimeOffset itemDateModifiedReal;
    public DateTimeOffset ItemDateModifiedReal
    {
        get => itemDateModifiedReal;
        set
        {
            ItemDateModified = dateTimeFormatter.ToShortLabel(value);
            itemDateModifiedReal = value;
            OnPropertyChanged(nameof(ItemDateModified));
        }
    }

    private DateTimeOffset itemDateCreatedReal;
    public DateTimeOffset ItemDateCreatedReal
    {
        get => itemDateCreatedReal;
        set
        {
            ItemDateCreated = dateTimeFormatter.ToShortLabel(value);
            itemDateCreatedReal = value;
            OnPropertyChanged(nameof(ItemDateCreated));
        }
    }

    private DateTimeOffset itemDateAccessedReal;
    public DateTimeOffset ItemDateAccessedReal
    {
        get => itemDateAccessedReal;
        set
        {
            ItemDateAccessed = dateTimeFormatter.ToShortLabel(value);
            itemDateAccessedReal = value;
            OnPropertyChanged(nameof(ItemDateAccessed));
        }
    }

    private ObservableCollection<FileProperty> itemProperties;
    public ObservableCollection<FileProperty> ItemProperties
    {
        get => itemProperties;
        set => SetProperty(ref itemProperties, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListedItem" /> class.
    /// </summary>
    /// <param name="folderRelativeId"></param>
    public ListedItem(string folderRelativeId)
    {
        FolderRelativeId = folderRelativeId;
    }

    // Parameterless constructor for JsonConvert
    public ListedItem()
    {
    }

    private ObservableCollection<FileProperty> fileDetails;
    public ObservableCollection<FileProperty> FileDetails
    {
        get => fileDetails;
        set => SetProperty(ref fileDetails, value);
    }

    public override string ToString()
    {
        string suffix;
        if (IsShortcut)
        {
            suffix = "ShortcutItemAutomation".GetLocalizedResource();
        }
        else
        {
            suffix = PrimaryItemAttribute == StorageItemTypes.File ? "Folder".GetLocalizedResource() : "FolderItemAutomation".GetLocalizedResource();
        }

        return $"{Name}, {suffix}";
    }

    public bool IsFolder => PrimaryItemAttribute is StorageItemTypes.Folder;
    public bool IsShortcut => this is ShortcutItem;
    public bool IsLinkItem => IsShortcut && ((ShortcutItem)this).IsUrl;
    public bool IsFtpItem => this is FtpItem;
    public bool IsArchive => this is ZipItem;
    public virtual bool IsExecutable => FileExtensionHelpers.IsExecutableFile(ItemPath);
    //public bool IsDriveRoot => ItemPath == PathNormalization.GetPathRoot(ItemPath);
    //public bool IsElevated => CheckElevationRights();

    private BaseStorageFile itemFile;
    public BaseStorageFile ItemFile
    {
        get => itemFile;
        set => SetProperty(ref itemFile, value);
    }

    public string Key
    {
        get; set;
    }
}

public class FtpItem : ListedItem
{
    public FtpItem(FtpListItem item, string folder) : base(null!)
    {
        var isFile = item.Type == FtpObjectType.File;
        ItemDateCreatedReal = item.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawCreated;
        ItemDateModifiedReal = item.RawModified < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawModified;
        ItemNameRaw = item.Name;
        FileExtension = Path.GetExtension(item.Name);
        ItemPath = PathNormalization.Combine(folder, item.Name);
        PrimaryItemAttribute = isFile ? StorageItemTypes.File : StorageItemTypes.Folder;
        ItemPropertiesInitialized = false;

        var itemType = isFile ? "File".GetLocalizedResource() : "Folder".GetLocalizedResource();
        if (isFile && Name.Contains('.', StringComparison.Ordinal))
        {
            itemType = FileExtension.Trim('.') + " " + itemType;
        }

        ItemType = itemType;
        FileSizeBytes = item.Size;
        ContainsFilesOrFolders = !isFile;
        FileImage = null!;
        FileSize = isFile ? FileSizeBytes.ToSizeString() : null!;
        IsHiddenItem = false;
    }

    public async Task<IStorageItem> ToStorageItem() => PrimaryItemAttribute switch
    {
        StorageItemTypes.File => await new FtpStorageFile(ItemPath, ItemNameRaw, ItemDateCreatedReal).ToStorageFileAsync(),
        StorageItemTypes.Folder => new FtpStorageFolder(ItemPath, ItemNameRaw, ItemDateCreatedReal),
        _ => throw new InvalidDataException(),
    };
}

public class ShortcutItem : ListedItem
{
    public ShortcutItem(string folderRelativeId) : base(folderRelativeId)
    {
    }

    // Parameterless constructor for JsonConvert
    public ShortcutItem() : base()
    {
    }

    // For shortcut elements (.lnk and .url)
    public string TargetPath
    {
        get; set;
    }

    public override string Name
        => IsSymLink ? base.Name : Path.GetFileNameWithoutExtension(ItemNameRaw); // Always hide extension for shortcuts

    public string Arguments
    {
        get; set;
    }
    public string WorkingDirectory
    {
        get; set;
    }
    public bool RunAsAdmin
    {
        get; set;
    }
    public bool IsUrl
    {
        get; set;
    }
    public bool IsSymLink
    {
        get; set;
    }
    public override bool IsExecutable => FileExtensionHelpers.IsExecutableFile(TargetPath, true);
}

public class ZipItem : ListedItem
{
    public ZipItem(string folderRelativeId) : base(folderRelativeId)
    {
    }

    public override string Name
    {
        get
        {
            /*var nameWithoutExtension = Path.GetFileNameWithoutExtension(ItemNameRaw);
            if (!string.IsNullOrEmpty(nameWithoutExtension) && !UserSettingsService.FoldersSettingsService.ShowFileExtensions)
            {
                return nameWithoutExtension;
            }*/
            return ItemNameRaw;
        }
    }

    // Parameterless constructor for JsonConvert
    public ZipItem() : base()
    {
    }
}
