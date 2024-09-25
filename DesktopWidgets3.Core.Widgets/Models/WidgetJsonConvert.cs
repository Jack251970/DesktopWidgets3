using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Windows.Graphics;

namespace DesktopWidgets3.Core.Widgets.Models;

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

        var widgetName = jsonObject["Name"]?.Value<string>() ?? string.Empty;
        var widgetId = jsonObject["Id"]?.Value<string>() ?? StringUtils.GetRandomWidgetId();
        var indexTag = jsonObject["IndexTag"]?.Value<int>() ?? new Random().Next(100, 999);
        var pinned = jsonObject["Pinned"]?.Value<bool>() ?? false;
        var position = jsonObject["Position"]?.ToObject<PointInt32>(serializer) ?? new PointInt32(-1, -1);
        var size = jsonObject["Size"]?.ToObject<RectSize>(serializer) ?? new RectSize(318, 200);
        var displayMonitor = jsonObject["DisplayMonitor"]?.ToObject<DisplayMonitor>(serializer) ?? DisplayMonitor.GetPrimaryMonitorInfo();
        var defaultWidgetSettings = _widgetResourceService.GetDefaultSetting(widgetId);
        var widgetSettings = jsonObject["Settings"]?.ToObject(defaultWidgetSettings.GetType(), serializer) as BaseWidgetSettings ?? defaultWidgetSettings;
        var settingsJToken = jsonObject["Settings"]?.DeepClone();

        return new JsonWidgetItem
        {
            Name = widgetName,
            Id = widgetId,
            IndexTag = indexTag,
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
            new JProperty("Name", widgetItem.Name),
            new JProperty("Id", widgetId),
            new JProperty("IndexTag", widgetItem.IndexTag),
            new JProperty("Pinned", widgetItem.Pinned),
            new JProperty("Position", JToken.FromObject(widgetItem.Position, serializer)),
            new JProperty("Size", JToken.FromObject(widgetItem.Size, serializer)),
            new JProperty("DisplayMonitor", JToken.FromObject(widgetItem.DisplayMonitor, serializer))
        );

        if (_widgetResourceService.IsWidgetUnknown(widgetItem.Id))
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
