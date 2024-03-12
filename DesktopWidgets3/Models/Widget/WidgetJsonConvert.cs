using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Windows.Graphics;

namespace DesktopWidgets3.Models.Widget;

internal class JsonWidgetItemConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(JsonWidgetItem);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);

        // phase settings
        var widgetType = (WidgetType)Enum.Parse(typeof(WidgetType), jsonObject["Type"]?.Value<string>() ?? WidgetType.Clock.ToString());
        var indexTag = jsonObject["IndexTag"]?.Value<int>() ?? new Random().Next(100, 999);
        var isEnabled = jsonObject["IsEnabled"]?.Value<bool>() ?? false;
        var position = jsonObject["Position"]?.ToObject<PointInt32>(serializer) ?? new PointInt32(-1, -1);
        var size = jsonObject["Size"]?.ToObject<RectSize>(serializer) ?? WidgetResourceService.GetDefaultSize(widgetType);
        var displayMonitor = jsonObject["DisplayMonitor"]?.ToObject<DisplayMonitor>(serializer) ?? new(WidgetManagerService.GetMonitorInfo(null));
        var widgetSettings = (BaseWidgetSettings?)(widgetType switch
        {
            WidgetType.Clock => jsonObject["Settings"]?.ToObject<ClockWidgetSettings>(serializer),
            WidgetType.Performance => jsonObject["Settings"]?.ToObject<PerformanceWidgetSettings>(serializer),
            WidgetType.Disk => jsonObject["Settings"]?.ToObject<DiskWidgetSettings>(serializer),
            WidgetType.FolderView => jsonObject["Settings"]?.ToObject<FolderViewWidgetSettings>(serializer),
            WidgetType.Network => jsonObject["Settings"]?.ToObject<NetworkWidgetSettings>(serializer),
            _ => throw new ArgumentOutOfRangeException(nameof(reader), reader, null)
        }) ?? WidgetResourceService.GetDefaultSettings(widgetType);

        return new JsonWidgetItem
        {
            Type = widgetType,
            IndexTag = indexTag,
            IsEnabled = isEnabled,
            Position = position,
            Size = size,
            DisplayMonitor = displayMonitor,
            Settings = widgetSettings
        };
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var widgetItem = value as JsonWidgetItem;

        var jsonObject = new JObject(
            new JProperty("Type", widgetItem!.Type.ToString()),
            new JProperty("IndexTag", widgetItem.IndexTag),
            new JProperty("IsEnabled", widgetItem.IsEnabled),
            new JProperty("Position", JToken.FromObject(widgetItem.Position, serializer)),
            new JProperty("Size", JToken.FromObject(widgetItem.Size, serializer)),
            new JProperty("DisplayMonitor", JToken.FromObject(widgetItem.DisplayMonitor, serializer)),
            new JProperty("Settings", JToken.FromObject(widgetItem.Settings, serializer))
        );

        jsonObject.WriteTo(writer);
    }
}
