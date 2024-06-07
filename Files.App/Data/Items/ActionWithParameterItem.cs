// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items;

[Serializable]
[method: JsonConstructor]
public class ActionWithParameterItem(string commandCode, string keyBinding, string? commandParameter = null)
{
    [JsonPropertyName("CommandCode")]
    public string CommandCode { get; set; } = commandCode;

    [JsonPropertyName("CommandParameter")]
    public string CommandParameter { get; set; } = commandParameter ?? string.Empty;

    [JsonPropertyName("KeyBinding")]
    public string KeyBinding { get; set; } = keyBinding;
}
