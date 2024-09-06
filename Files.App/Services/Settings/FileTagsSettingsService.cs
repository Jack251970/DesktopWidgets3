// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Serialization.Implementation;
using System.IO;

namespace Files.App.Services.Settings;

internal sealed class FileTagsSettingsService : BaseJsonSettings, IFileTagsSettingsService
{
    private static string ClassName => typeof(FileTagsSettingsService).Name;

    public event EventHandler? OnSettingImportedEvent;

	public event EventHandler? OnTagsUpdated;

	private static readonly List<TagViewModel> DefaultFileTags =
    [
        new("Home", "#0072BD", "f7e0e137-2eb5-4fa4-a50d-ddd65df17c34"),
		new("Work", "#D95319", "c84a8131-c4de-47d9-9440-26e859d14b3d"),
		new("Photos", "#EDB120", "d4b8d4bd-ceaf-4e58-ac61-a185fcf96c5d"),
		new("Important", "#77AC30", "79376daf-c44a-4fe4-aa3b-8b30baea453e")
	];

	public FileTagsSettingsService()
	{
		SettingsSerializer = new DefaultSettingsSerializer();
		JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
		JsonSettingsDatabase = new CachingJsonSettingsDatabase(SettingsSerializer, JsonSettingsSerializer);

		Initialize(Path.Combine(LocalSettingsExtensions.GetApplicationDataFolder("Files"),
			Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.FileTagSettingsFileName));
	}

    public void Initialize(IUserSettingsService userSettingsService) => throw new NotImplementedException();

    public IList<TagViewModel> FileTagList
	{
		get
		{
			var tags = Get(DefaultFileTags);

			foreach (var tag in tags!)
            {
                tag.Color = ColorHelpers.FromHex(tag.Color).ToString();
            }

            return tags;
		}
		set
		{
			Set(value);
			OnTagsUpdated?.Invoke(this, EventArgs.Empty);
		}
	}

	public TagViewModel GetTagById(string uid)
	{
		if (FileTagList.Any(x => x.Uid is null))
		{
			LogExtensions.LogWarning(ClassName, "Tags file is invalid, regenerate");
			FileTagList = DefaultFileTags;
		}

		var tag = FileTagList.SingleOrDefault(x => x.Uid == uid);

		if (!string.IsNullOrEmpty(uid) && tag is null)
		{
			tag = new TagViewModel("Unknown".GetLocalizedResource(), "#9ea3a1", uid);
			FileTagList = FileTagList.Append(tag).ToList();
		}

		return tag!;
	}

	public IList<TagViewModel>? GetTagsByIds(string[] uids)
	{
		return uids is null || uids.Length == 0
			? null
			: uids.Select(GetTagById).Where(x => x is not null).ToList();
	}

	public IEnumerable<TagViewModel> GetTagsByName(string tagName)
	{
		return FileTagList.Where(x => x.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
	}

	public void CreateNewTag(string newTagName, string color)
	{
		var newTag = new TagViewModel(
			newTagName,
			color,
			Guid.NewGuid().ToString());

		var oldTags = FileTagList.ToList();
		oldTags.Add(newTag);
		FileTagList = oldTags;
	}

	public void EditTag(string uid, string name, string color)
	{
        var index = GetTagIndex(uid);
        if (index == -1)
        {
            return;
        }

        var oldTags = FileTagList.ToList();
        oldTags.RemoveAt(index);
        oldTags.Insert(index, new TagViewModel(name, color, uid));
        FileTagList = oldTags;
    }

    public void DeleteTag(IFolderViewViewModel folderViewViewModel, string uid)
	{
        var index = GetTagIndex(uid);
        if (index == -1)
        {
            return;
        }

        var oldTags = FileTagList.ToList();
		oldTags.RemoveAt(index);
		FileTagList = oldTags;
        UntagAllFiles(folderViewViewModel, uid);
	}

	public override bool ImportSettings(object import)
	{
		if (import is string importString)
		{
			FileTagList = JsonSettingsSerializer!.DeserializeFromJson<List<TagViewModel>>(importString)!;
		}
		else if (import is List<TagViewModel> importList)
		{
			FileTagList = importList;
		}

		FileTagList ??= DefaultFileTags;

		if (FileTagList is not null)
		{
			FlushSettings();
			OnSettingImportedEvent?.Invoke(this, null!);
			return true;
		}

		return false;
	}

	public override object ExportSettings()
	{
        var settings = new Dictionary<string, object>
        {
            { "FileTagList", FileTagList }
        };

        // Serialize settings to JSON format
        return JsonSettingsSerializer!.SerializeToJson(settings)!;
    }

    private int GetTagIndex(string uid)
    {
        for (var i = 0; i < FileTagList.Count; i++)
        {
            if (FileTagList[i].Uid == uid)
            {
                return i;
            }
        }

        return -1;
    }

    private static void UntagAllFiles(IFolderViewViewModel folderViewViewModel, string uid)
	{
		var tagDoDelete = new string[] { uid };

		foreach (var item in FileTagsHelper.GetDbInstance().GetAll())
		{
			if (item.Tags.Contains(uid))
			{
				FileTagsHelper.WriteFileTag(
                    folderViewViewModel,
					item.FilePath,
					item.Tags.Except(tagDoDelete).ToArray());
            }
		}
	}
}

