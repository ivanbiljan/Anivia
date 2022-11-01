using System;
using System.Text.Json.Serialization;

namespace Victoria.Payloads.Player;

internal sealed class SeekPayload : AbstractPlayerPayload {
    public SeekPayload(ulong guildId, TimeSpan position) : base(guildId, "seek") {
        Position = (long)position.TotalMilliseconds;
    }

    [JsonPropertyName("position")]
    public long Position { get; }
}