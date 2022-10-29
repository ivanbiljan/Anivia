using System;
using System.Text.Json.Serialization;
using Victoria.Converters;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Victoria.Node.EventArgs; 

/// <summary>
/// </summary>
public struct StatsEventArg {
    /// <summary>
    /// </summary>
    [JsonPropertyName("playingPlayers")]
    [JsonInclude]
    public int PlayingPlayers { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("players")]
    [JsonInclude]
    public int Players { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("uptime")]
    [JsonConverter(typeof(LongToTimeSpanConverter))]
    [JsonInclude]
    public TimeSpan Uptime { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("memory")]
    [JsonInclude]
    public Memory Memory { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("cpu")]
    [JsonInclude]
    public Cpu Cpu { get; private set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("frames")]
    [JsonInclude]
    public Frames Frames { get; private set; }
}