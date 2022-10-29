using System.Text.Json.Serialization;

namespace Victoria.Node.EventArgs;

/// <summary>
/// </summary>
public struct Memory {
    /// <summary>
    /// </summary>
    [JsonPropertyName("reservable")]
    [JsonInclude]
    public ulong Reservable { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("used")]
    [JsonInclude]
    public ulong Used { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("allocated")]
    [JsonInclude]
    public ulong Allocated { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("free")]
    [JsonInclude]
    public ulong Free { get; private set; }
}