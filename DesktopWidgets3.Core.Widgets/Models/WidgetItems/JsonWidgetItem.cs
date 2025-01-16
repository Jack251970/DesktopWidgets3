using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Graphics;

namespace DesktopWidgets3.Core.Widgets.Models.WidgetItems;

[JsonConverter(typeof(JsonWidgetItemConverter))]
public class JsonWidgetItem : BaseWidgetItem
{
    public required WidgetProviderType ProviderType { get; set; }

    public required string Name { get; set; }

    public required PointInt32 Position { get; set; }

    public required RectSize Size { get; set; }

    public required DisplayMonitor DisplayMonitor { get; set; }

    public new required BaseWidgetSettings Settings
    {
        get => base.Settings;
        set => base.Settings = value;
    }

    public JToken? SettingsJToken { get; set; }

    public bool Equals(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        return ProviderType == providerType && Id == widgetId && Type == widgetType && Index == widgetIndex;
    }

    public bool Equals(WidgetProviderType providerType, string widgetId, string widgetType)
    {
        return ProviderType == providerType && Id == widgetId && Type == widgetType;
    }
}

public class JsonWidgetItemConverter : JsonConverter
{
    private readonly IWidgetResourceService _widgetResourceService = DependencyExtensions.GetRequiredService<IWidgetResourceService>();

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(JsonWidgetItem);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        
        var widgetProviderType = jsonObject["ProviderType"]?.ToObject<WidgetProviderType>(serializer) ?? WidgetProviderType.DesktopWidgets3;
        var widgetName = jsonObject["Name"]?.Value<string>() ?? string.Empty;
        var widgetId = jsonObject["Id"]?.Value<string>() ?? StringUtils.GetRandomWidgetId();
        var widgetType = jsonObject["Type"]?.Value<string>() ?? string.Empty;
        var widgetIndex = jsonObject["Index"]?.Value<int>() ?? new Random().Next(100, 999);
        var pinned = jsonObject["Pinned"]?.Value<bool>() ?? false;
        var position = jsonObject["Position"]?.ToObject<PointInt32>(serializer) ?? WidgetConstants.DefaultWidgetPosition;
        var size = jsonObject["Size"]?.ToObject<RectSize>(serializer) ?? WidgetConstants.DefaultWidgetSize;
        var displayMonitor = jsonObject["DisplayMonitor"]?.ToObject<DisplayMonitor>(serializer) ?? DisplayMonitor.GetPrimaryMonitorInfo();
        var defaultWidgetSettings = _widgetResourceService.GetDefaultSettings(widgetId, widgetType);
        var widgetSettings = jsonObject["Settings"]?.ToObject(defaultWidgetSettings.GetType(), serializer) as BaseWidgetSettings ?? defaultWidgetSettings;
        var settingsJToken = jsonObject["Settings"]?.DeepClone();

        return new JsonWidgetItem
        {
            ProviderType = widgetProviderType,
            Name = widgetName,
            Id = widgetId,
            Type = widgetType,
            Index = widgetIndex,
            Pinned = pinned,
            Position = position,
            Size = size,
            DisplayMonitor = displayMonitor,
            Settings = widgetSettings,
            SettingsJToken = settingsJToken
        };
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var widgetItem = value as JsonWidgetItem;

        var widgetId = widgetItem!.Id;
        var jsonObject = new JObject(
            new JProperty("ProviderType", widgetItem.ProviderType),
            new JProperty("Name", widgetItem.Name),
            new JProperty("Id", widgetId),
            new JProperty("Type", widgetItem.Type),
            new JProperty("Index", widgetItem.Index),
            new JProperty("Pinned", widgetItem.Pinned),
            new JProperty("Position", JToken.FromObject(widgetItem.Position, serializer)),
            new JProperty("Size", JToken.FromObject(widgetItem.Size, serializer)),
            new JProperty("DisplayMonitor", JToken.FromObject(widgetItem.DisplayMonitor, serializer))
        );

        if (_widgetResourceService.IsWidgetGroupUnknown(widgetItem.ProviderType, widgetItem.Id, widgetItem.Type))
        {
            jsonObject.Add(new JProperty("Settings", widgetItem.SettingsJToken));
        }
        else
        {
            jsonObject.Add(new JProperty("Settings", JToken.FromObject(widgetItem.Settings, serializer)));
        }

        jsonObject.WriteTo(writer);
    }
}
