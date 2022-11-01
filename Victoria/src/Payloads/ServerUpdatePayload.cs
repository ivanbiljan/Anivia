using System.Text.Json.Serialization;

namespace Victoria.Payloads;

internal sealed class ServerUpdatePayload : AbstractPayload {
    public ServerUpdatePayload() : base("voiceUpdate") { }

    [JsonPropertyName("guildId")]
    public string GuildId { get; init; }

    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; }

    [JsonPropertyName("event")]
    public VoiceServerPayload VoiceServerPayload { get; init; }
}