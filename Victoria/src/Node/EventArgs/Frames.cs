using System.Text.Json.Serialization;

namespace Victoria.Node.EventArgs;

/// <summary>
/// </summary>
public struct Frames {
    /// <summary>
    /// </summary>
    [JsonPropertyName("sent")]
    [JsonInclude]
    public int Sent { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("nulled")]
    [JsonInclude]
    public int Nulled { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("deficit")]
    [JsonInclude]
    public int Deficit { get; private set; }
}