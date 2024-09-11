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

        var widgetId = jsonObject["Id"]?.Value<string>() ?? StringUtils.GetRandomWidgetId();
        var indexTag = jsonObject["IndexTag"]?.Value<int>() ?? new Random().Next(100, 999);
        var isEnabled = jsonObject["IsEnabled"]?.Value<bool>() ?? false;
        var position = jsonObject["Position"]?.ToObject<PointInt32>(serializer) ?? new PointInt32(-1, -1);
        var size = jsonObject["Size"]?.ToObject<RectSize>(serializer) ?? new RectSize(318, 200);
        var displayMonitor = jsonObject["DisplayMonitor"]?.ToObject<DisplayMonitor>(serializer) ?? DisplayMonitor.GetPrimaryMonitorInfo();
        var defaultWidgetSettings = _widgetResourceService.GetDefaultSetting(widgetId);
        var widgetSettings = jsonObject["Settings"]?.ToObject(defaultWidgetSettings.GetType(), serializer) as BaseWidgetSettings ?? defaultWidgetSettings;

        return new JsonWidgetItem
        {
            Id = widgetId,
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
            new JProperty("Id", widgetItem!.Id),
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
