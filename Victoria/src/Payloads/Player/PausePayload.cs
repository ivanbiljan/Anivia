using System.Text.Json.Serialization;

namespace Victoria.Payloads.Player;

internal sealed class PausePayload : AbstractPlayerPayload {
    public PausePayload(ulong guildId, bool pause) : base(guildId, "pause") {
        Pause = pause;
    }

    [JsonPropertyName("pause")]
    public bool Pause { get; }
}