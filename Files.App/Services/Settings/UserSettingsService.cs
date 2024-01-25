// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Serialization.Implementation;
using System.IO;

namespace Files.App.Services.Settings;

// TODO: change to internal.
public sealed class UserSettingsService : BaseJsonSettings, IUserSettingsService
{
	private IGeneralSettingsService _GeneralSettingsService = null!;
    public IGeneralSettingsService GeneralSettingsService => GetSettingsService(ref _GeneralSettingsService);

    private IFoldersSettingsService _FoldersSettingsService = null!;
    public IFoldersSettingsService FoldersSettingsService => GetSettingsService(ref _FoldersSettingsService);

    private IAppearanceSettingsService _AppearanceSettingsService = null!;
    public IAppearanceSettingsService AppearanceSettingsService => GetSettingsService(ref _AppearanceSettingsService);

    private IInfoPaneSettingsService _InfoPaneSettingsService = null!;
    public IInfoPaneSettingsService InfoPaneSettingsService => GetSettingsService(ref _InfoPaneSettingsService);

    private ILayoutSettingsService _LayoutSettingsService = null!;
    public ILayoutSettingsService LayoutSettingsService => GetSettingsService(ref _LayoutSettingsService);

    private IApplicationSettingsService _ApplicationSettingsService = null!;
    public IApplicationSettingsService ApplicationSettingsService => GetSettingsService(ref _ApplicationSettingsService);

    private IAppSettingsService _AppSettingsService = null!;
    public IAppSettingsService AppSettingsService => GetSettingsService(ref _AppSettingsService);

    public UserSettingsService()
	{
        SettingsSerializer = new DefaultSettingsSerializer();
		JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
		JsonSettingsDatabase = new CachingJsonSettingsDatabase(SettingsSerializer, JsonSettingsSerializer);

        Initialize(Path.Combine(LocalSettingsExtensions.ApplicationDataFolder, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.UserSettingsFileName));
	}

	public override object ExportSettings()
	{
		var export = (IDictionary<string, object>)base.ExportSettings();

		// Remove session settings
		export.Remove(nameof(GeneralSettingsService.LastSessionTabList));
		export.Remove(nameof(GeneralSettingsService.LastCrashedTabList));

		return JsonSettingsSerializer!.SerializeToJson(export)!;
	}

	public override bool ImportSettings(object import)
	{
		var settingsImport = import switch
		{
			string s => JsonSettingsSerializer?.DeserializeFromJson<Dictionary<string, object>>(s) ?? new(),
			Dictionary<string, object> d => d,
			_ => new(),
		};

		if (!settingsImport.IsEmpty() && base.ImportSettings(settingsImport))
		{
			foreach (var item in settingsImport)
			{
				RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(item.Key, item.Value));
			}

			return true;
		}

		return false;
	}

	private static TSettingsService GetSettingsService<TSettingsService>(ref TSettingsService settingsServiceMember)
		where TSettingsService : class, IBaseSettingsService
	{
		settingsServiceMember ??= DependencyExtensions.GetService<TSettingsService>()!;

		return settingsServiceMember;
	}
}
