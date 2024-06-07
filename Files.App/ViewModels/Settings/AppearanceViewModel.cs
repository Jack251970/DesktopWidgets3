// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;
using Windows.Storage.Pickers;

namespace Files.App.ViewModels.Settings;

public sealed class AppearanceViewModel : ObservableObject
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    private IAppThemeModeService AppThemeModeService { get; } = DependencyExtensions.GetService<IAppThemeModeService>();
    private IUserSettingsService UserSettingsService { get; set; } = null!;
    private IResourcesService ResourcesService { get; set; } = null!;

    public List<string> Themes { get; private set; }
    public Dictionary<BackdropMaterialType, string> BackdropMaterialTypes { get; private set; } = [];

    public Dictionary<Stretch, string> ImageStretchTypes { get; private set; } = [];

    public Dictionary<VerticalAlignment, string> ImageVerticalAlignmentTypes { get; private set; } = [];

    public Dictionary<HorizontalAlignment, string> ImageHorizontalAlignmentTypes { get; private set; } = [];

    public ObservableCollection<AppThemeResourceItem> AppThemeResources { get; }

    public ICommand SelectImageCommand { get; } = null!;
    public ICommand RemoveImageCommand { get; } = null!;

    public AppearanceViewModel()
	{
        /*UserSettingsService = DependencyExtensions.GetService<IUserSettingsService>();*/
        ResourcesService = DependencyExtensions.GetService<IResourcesService>();

		Themes =
        [
            "Default".GetLocalizedResource(),
			"LightTheme".GetLocalizedResource(),
			"DarkTheme".GetLocalizedResource()
		];

		// FILESTODO: Re-add Solid and regular Mica when theming is revamped
		//BackdropMaterialTypes.Add(BackdropMaterialType.Solid, "Solid".GetLocalizedResource());

		BackdropMaterialTypes.Add(BackdropMaterialType.Acrylic, "Acrylic".GetLocalizedResource());

		//BackdropMaterialTypes.Add(BackdropMaterialType.Mica, "Mica".GetLocalizedResource());
		BackdropMaterialTypes.Add(BackdropMaterialType.MicaAlt, "MicaAlt".GetLocalizedResource());

		/*selectedBackdropMaterial = BackdropMaterialTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackdropMaterial];*/

		AppThemeResources = AppThemeResourceFactory.AppThemeResources;

        // Background image fit options
        ImageStretchTypes.Add(Stretch.None, "None".GetLocalizedResource());
        ImageStretchTypes.Add(Stretch.Fill, "Fill".GetLocalizedResource());
        ImageStretchTypes.Add(Stretch.Uniform, "Uniform".GetLocalizedResource());
        ImageStretchTypes.Add(Stretch.UniformToFill, "UniformToFill".GetLocalizedResource());
        /*SelectedImageStretchType = ImageStretchTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageFit];*/

        // Background image allignment options

        // VerticalAlignment
        ImageVerticalAlignmentTypes.Add(VerticalAlignment.Top, "Top".GetLocalizedResource());
        ImageVerticalAlignmentTypes.Add(VerticalAlignment.Center, "Center".GetLocalizedResource());
        ImageVerticalAlignmentTypes.Add(VerticalAlignment.Bottom, "Bottom".GetLocalizedResource());
        /*SelectedImageVerticalAlignmentType = ImageVerticalAlignmentTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment];*/

        // HorizontalAlignment
        ImageHorizontalAlignmentTypes.Add(HorizontalAlignment.Left, "Left".GetLocalizedResource());
        ImageHorizontalAlignmentTypes.Add(HorizontalAlignment.Center, "Center".GetLocalizedResource());
        ImageHorizontalAlignmentTypes.Add(HorizontalAlignment.Right, "Right".GetLocalizedResource());
        /*SelectedImageHorizontalAlignmentType = ImageHorizontalAlignmentTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment];*/

        /*UpdateSelectedResource();*/

        SelectImageCommand = new AsyncRelayCommand(SelectBackgroundImage);
        RemoveImageCommand = new RelayCommand(RemoveBackgroundImage);
    }

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
        UserSettingsService = folderViewViewModel.GetService<IUserSettingsService>();

        selectedBackdropMaterial = BackdropMaterialTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackdropMaterial];

        SelectedImageStretchType = ImageStretchTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageFit];

        SelectedImageVerticalAlignmentType = ImageVerticalAlignmentTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment];

        SelectedImageHorizontalAlignmentType = ImageHorizontalAlignmentTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment];

        UpdateSelectedResource();
    }

    /// <summary>
    /// Opens a file picker to select a background image
    /// </summary>
    private async Task SelectBackgroundImage()
    {
        var filePicker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            FileTypeFilter = { ".png", ".bmp", ".jpg", ".jpeg", ".jfif", ".gif", ".tiff", ".tif", ".webp" }
        };

        // WINUI3: Create and initialize new window
        var parentWindowId = FolderViewViewModel.AppWindow.Id;
        var handle = Microsoft.UI.Win32Interop.GetWindowFromWindowId(parentWindowId);
        WinRT.Interop.InitializeWithWindow.Initialize(filePicker, handle);

        var file = await filePicker.PickSingleFileAsync();
        if (file is not null)
        {
            AppThemeBackgroundImageSource = file.Path;
        }
    }

    /// <summary>
    /// Clears the current background image
    /// </summary>
    private void RemoveBackgroundImage()
    {
        AppThemeBackgroundImageSource = string.Empty;
    }

    /// <summary>
    /// Selects the AppThemeResource corresponding to the current settings
    /// </summary>
    private void UpdateSelectedResource()
    {
        var themeBackgroundColor = AppThemeBackgroundColor;

        // Add color to the collection if it's not already there
        if (!AppThemeResources.Any(p => p.BackgroundColor == themeBackgroundColor))
        {
            // Remove current value before adding a new one
            if (AppThemeResources.Last().Name == "Custom".GetLocalizedResource())
            {
                AppThemeResources.Remove(AppThemeResources.Last());
            }

            var appThemeBackgroundColor = new AppThemeResourceItem
            {
                BackgroundColor = themeBackgroundColor,
                Name = "Custom".GetLocalizedResource(),
            };

            AppThemeResources.Add(appThemeBackgroundColor);
        }

        SelectedAppThemeResources = AppThemeResources
            .FirstOrDefault(p => p.BackgroundColor == themeBackgroundColor) ?? AppThemeResources[0];
    }

    private AppThemeResourceItem selectedAppThemeResources = null!;
    public AppThemeResourceItem SelectedAppThemeResources
    {
        get => selectedAppThemeResources;
        set
        {
            if (value is not null && SetProperty(ref selectedAppThemeResources, value))
            {
                AppThemeBackgroundColor = SelectedAppThemeResources.BackgroundColor!;
                OnPropertyChanged(nameof(selectedAppThemeResources));
            }
        }
    }

    private int selectedThemeIndex;
    public int SelectedThemeIndex
    {
        get => selectedThemeIndex;
        set
        {
            if (SetProperty(ref selectedThemeIndex, value))
            {
                AppThemeModeService.AppThemeMode = (ElementTheme)value;
                OnPropertyChanged(nameof(SelectedElementTheme));
            }
        }
    }

    public ElementTheme SelectedElementTheme => (ElementTheme)selectedThemeIndex;

    public string AppThemeBackgroundColor
    {
        get => UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor;
        set
        {
            if (value != UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor)
            {
                UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor = value;

                // Apply the updated background resource
                try
                {
                    ResourcesService.SetAppThemeBackgroundColor(ColorHelper.ToColor(value).FromWindowsColor());
                }
                catch
                {
                    ResourcesService.SetAppThemeBackgroundColor(ColorHelper.ToColor("#00000000").FromWindowsColor());
                }
                ResourcesService.ApplyResources();

                OnPropertyChanged();
            }
        }
    }

    private string selectedBackdropMaterial = null!;
    public string SelectedBackdropMaterial
    {
        get => selectedBackdropMaterial;
        set
        {
            if (SetProperty(ref selectedBackdropMaterial, value))
            {
                UserSettingsService.AppearanceSettingsService.AppThemeBackdropMaterial = BackdropMaterialTypes.First(e => e.Value == value).Key;
            }
        }
    }

    public string AppThemeBackgroundImageSource
    {
        get => UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageSource;
        set
        {
            if (value != UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageSource)
            {
                UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageSource = value;

                OnPropertyChanged();
            }
        }
    }

    private string selectedImageStretchType = null!;
    public string SelectedImageStretchType
    {
        get => selectedImageStretchType;
        set
        {
            if (SetProperty(ref selectedImageStretchType, value))
            {
                UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageFit = ImageStretchTypes.First(e => e.Value == value).Key;
            }
        }
    }

    public float AppThemeBackgroundImageOpacity
    {
        get => UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageOpacity;
        set
        {
            if (value != UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageOpacity)
            {
                UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageOpacity = value;

                OnPropertyChanged();
            }
        }
    }

    private string selectedImageVerticalAlignmentType = null!;
    public string SelectedImageVerticalAlignmentType
    {
        get => selectedImageVerticalAlignmentType;
        set
        {
            if (SetProperty(ref selectedImageVerticalAlignmentType, value))
            {
                UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment = ImageVerticalAlignmentTypes.First(e => e.Value == value).Key;
            }
        }
    }

    private string selectedImageHorizontalAlignmentType = null!;
    public string SelectedImageHorizontalAlignmentType
    {
        get => selectedImageHorizontalAlignmentType;
        set
        {
            if (SetProperty(ref selectedImageHorizontalAlignmentType, value))
            {
                UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment = ImageHorizontalAlignmentTypes.First(e => e.Value == value).Key;
            }
        }
    }
}
