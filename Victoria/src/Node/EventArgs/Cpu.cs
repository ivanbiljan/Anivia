using System.Text.Json.Serialization;

namespace Victoria.Node.EventArgs;

/// <summary>
/// </summary>
public struct Cpu {
    /// <summary>
    /// </summary>
    [JsonPropertyName("cores")]
    [JsonInclude]
    public int Cores { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("systemLoad")]
    [JsonInclude]
    public double SystemLoad { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("lavalinkLoad")]
    [JsonInclude]
    public double LavalinkLoad { get; private set; }
}