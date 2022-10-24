using System.Text.Json.Serialization;

namespace Victoria.Payloads; 

internal record VoiceServerPayload {
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; init; }

    [JsonPropertyName("token")]
    public string Token { get; init; }
}