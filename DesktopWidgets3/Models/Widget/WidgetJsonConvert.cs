using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Windows.Graphics;

namespace DesktopWidgets3.Models.Widget;

public class JsonWidgetItemConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(JsonWidgetItem);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);

        var widgetType = (WidgetType)Enum.Parse(typeof(WidgetType), jsonObject["Type"]!.Value<string>()!);
        var widgetItem = new JsonWidgetItem
        {
            Type = widgetType,
            IndexTag = jsonObject["IndexTag"]!.Value<int>(),
            IsEnabled = jsonObject["IsEnabled"]!.Value<bool>(),
            Position = jsonObject["Position"]!.ToObject<PointInt32>(serializer),
            Size = jsonObject["Size"]!.ToObject<WidgetSize>(serializer),
            Settings = widgetType switch
            {
                WidgetType.Clock => jsonObject["Settings"]!.ToObject<ClockWidgetSettings>(serializer)!,
                WidgetType.Performance => jsonObject["Settings"]!.ToObject<PerformanceWidgetSettings>(serializer)!,
                WidgetType.Disk => jsonObject["Settings"]!.ToObject<DiskWidgetSettings>(serializer)!,
                WidgetType.FolderView => jsonObject["Settings"]!.ToObject<FolderViewWidgetSettings>(serializer)!,
                WidgetType.Network => jsonObject["Settings"]!.ToObject<NetworkWidgetSettings>(serializer)!,
                _ => throw new ArgumentOutOfRangeException(nameof(reader), reader, null)
            },
        };

        return widgetItem;
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
            new JProperty("Settings", JToken.FromObject(widgetItem.Settings, serializer))
        );

        jsonObject.WriteTo(writer);
    }
}
