// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Models;

[Serializable]
[method: JsonConstructor]
public sealed partial class TagViewModel(string name, string color, string uid) : ObservableObject
{
    [JsonPropertyName("TagName")]
    public string Name { get; set; } = name;

    [JsonPropertyName("ColorString")]
    public string Color { get; set; } = color;

    [JsonPropertyName("Uid")]
    public string Uid { get; set; } = uid;
}
